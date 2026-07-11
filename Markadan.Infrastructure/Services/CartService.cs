using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Carts;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class CartService : ICartService
{
    private readonly MarkadanDbContext _db;

    public CartService(MarkadanDbContext db) => _db = db;

    public async Task<CartDTO> GetActiveCartAsync(int userId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateActiveCartAsync(userId, ct);
        return await BuildCartDTOAsync(cart.Id, ct);
    }

    public async Task<CartDTO> AddItemAsync(int userId, AddCartItemDTO dto, CancellationToken ct = default)
    {
        var cart = await GetOrCreateActiveCartAsync(userId, ct);

        var product = await _db.Products
            .Where(p => p.Id == dto.ProductId)
            .Select(p => new { p.Id, p.Price, p.Stock, HasVariants = p.Variants.Any() })
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("Ürün bulunamadı.");

        decimal snapshotPrice;
        int? variantId = null;

        if (product.HasVariants)
        {
            // Varyantlı ürün — varyant seçimi zorunlu
            if (dto.ProductVariantId is null)
                throw new BusinessRuleException("Bu ürün için bir seçenek (varyant) belirtmelisiniz.");

            var variant = await _db.ProductVariants
                .Where(v => v.Id == dto.ProductVariantId.Value && v.ProductId == dto.ProductId && v.IsActive)
                .Select(v => new { v.Id, v.Price, v.Stock })
                .FirstOrDefaultAsync(ct)
                ?? throw new BusinessRuleException("Seçilen varyant bulunamadı.");

            if (variant.Stock <= 0)
                throw new BusinessRuleException("Seçilen varyant stokta yok.");

            variantId      = variant.Id;
            snapshotPrice  = variant.Price ?? product.Price;  // varyant özel fiyatı yoksa ürün fiyatı
        }
        else
        {
            // Basit ürün — varyant gönderilmemeli
            if (dto.ProductVariantId is not null)
                throw new BusinessRuleException("Bu ürünün varyantı yok.");

            if (product.Stock <= 0)
                throw new BusinessRuleException("Ürün stokta yok.");

            snapshotPrice = product.Price;
        }

        var existing = await _db.CartItems
            .Where(i => i.CartId == cart.Id && i.ProductId == dto.ProductId && i.ProductVariantId == variantId)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            existing.Quantity += dto.Quantity;
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                CartId             = cart.Id,
                ProductId          = dto.ProductId,
                ProductVariantId   = variantId,
                Quantity           = dto.Quantity,
                UnitPriceSnapshot  = snapshotPrice   // sepete eklendiğindeki fiyat kilitlenir
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await BuildCartDTOAsync(cart.Id, ct);
    }

    public async Task<CartDTO> UpdateItemQuantityAsync(int userId, int cartItemId, int quantity, CancellationToken ct = default)
    {
        var cartId = await GetActiveCartIdAsync(userId, ct);

        var item = await _db.CartItems
            .Where(i => i.Id == cartItemId && i.CartId == cartId)
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("Sepet satırı bulunamadı.");

        if (quantity == 0)
            _db.CartItems.Remove(item);
        else
            item.Quantity = quantity;

        await UpdateCartTimestampAsync(cartId, ct);
        await _db.SaveChangesAsync(ct);

        return await BuildCartDTOAsync(cartId, ct);
    }

    public async Task<CartDTO> RemoveItemAsync(int userId, int cartItemId, CancellationToken ct = default)
    {
        var cartId = await GetActiveCartIdAsync(userId, ct);

        var deleted = await _db.CartItems
            .Where(i => i.Id == cartItemId && i.CartId == cartId)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
            throw new BusinessRuleException("Sepet satırı bulunamadı.");

        await UpdateCartTimestampAsync(cartId, ct);

        return await BuildCartDTOAsync(cartId, ct);
    }

    public async Task<CartDTO> ClearAsync(int userId, CancellationToken ct = default)
    {
        var cartId = await GetActiveCartIdAsync(userId, ct);

        await _db.CartItems
            .Where(i => i.CartId == cartId)
            .ExecuteDeleteAsync(ct);

        await UpdateCartTimestampAsync(cartId, ct);

        return await BuildCartDTOAsync(cartId, ct);
    }

    public async Task<CartDTO> AcceptPriceChangesAsync(int userId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateActiveCartAsync(userId, ct);

        var items = await _db.CartItems
            .Where(i => i.CartId == cart.Id)
            .Include(i => i.Product)
            .Include(i => i.ProductVariant)
            .ToListAsync(ct);

        bool changed = false;
        foreach (var item in items)
        {
            // Etkin güncel fiyat: varyant varsa varyant fiyatı (yoksa ürün fiyatı)
            var currentPrice = item.ProductVariant is not null
                ? (item.ProductVariant.Price ?? item.Product.Price)
                : item.Product.Price;

            if (item.UnitPriceSnapshot != currentPrice)
            {
                item.UnitPriceSnapshot = currentPrice;
                changed = true;
            }
        }

        if (changed)
        {
            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return await BuildCartDTOAsync(cart.Id, ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<Cart> GetOrCreateActiveCartAsync(int userId, CancellationToken ct)
    {
        var cart = await _db.Carts
            .Where(c => c.AppUserId == userId && c.Status == CartStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (cart is not null) return cart;

        cart = new Cart
        {
            AppUserId = userId,
            AppUser   = default!,
            Status    = CartStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(ct);
        return cart;
    }

    private async Task<int> GetActiveCartIdAsync(int userId, CancellationToken ct)
    {
        var cartId = await _db.Carts
            .Where(c => c.AppUserId == userId && c.Status == CartStatus.Active)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(ct);

        return cartId ?? throw new BusinessRuleException("Aktif sepet bulunamadı.");
    }

    private async Task UpdateCartTimestampAsync(int cartId, CancellationToken ct)
    {
        await _db.Carts
            .Where(c => c.Id == cartId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UpdatedAt, DateTime.UtcNow), ct);
    }

    private async Task<CartDTO> BuildCartDTOAsync(int cartId, CancellationToken ct)
    {
        var cart = await _db.Carts
            .AsNoTracking()
            .Where(c => c.Id == cartId)
            .Select(c => new
            {
                c.Id,
                c.Status,
                c.CouponCode,
                c.DiscountAmount,
                Items = c.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.ProductVariantId,
                    ProductTitle    = i.Product.Title,
                    ProductImageUrl = i.Product.ImageUrl,
                    VariantImageUrl = i.ProductVariant != null ? i.ProductVariant.ImageUrl : null,
                    i.UnitPriceSnapshot,
                    // Güncel etkin fiyat: varyant varsa varyant fiyatı (yoksa ürün fiyatı), değilse ürün fiyatı
                    CurrentPrice    = i.ProductVariant != null
                        ? (i.ProductVariant.Price ?? i.Product.Price)
                        : i.Product.Price,
                    // Varyant etiketi: seçili değerler (Kırmızı / M)
                    VariantValues   = i.ProductVariant != null
                        ? i.ProductVariant.SelectedValues
                            .OrderBy(sv => sv.OptionValue.Option.SortOrder)
                            .Select(sv => sv.OptionValue.Value)
                            .ToList()
                        : null,
                    i.Quantity
                }).ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Sepet bulunamadı.");

        var items = cart.Items.Select(i =>
        {
            var priceChanged = i.UnitPriceSnapshot != i.CurrentPrice;
            return new CartItemDTO(
                Id:                i.Id,
                ProductId:         i.ProductId,
                Title:             i.ProductTitle,
                ImageUrl:          i.VariantImageUrl ?? i.ProductImageUrl,
                UnitPriceSnapshot: i.UnitPriceSnapshot,
                CurrentPrice:      i.CurrentPrice,
                PriceChanged:      priceChanged,
                Quantity:          i.Quantity,
                Subtotal:          i.UnitPriceSnapshot * i.Quantity,
                VariantId:         i.ProductVariantId,
                VariantLabel:      i.VariantValues != null && i.VariantValues.Count > 0
                                       ? string.Join(" / ", i.VariantValues)
                                       : null
            );
        }).ToList();

        return new CartDTO(
            Id:              cart.Id,
            Status:          cart.Status.ToString(),
            Items:           items,
            Total:           items.Sum(i => i.Subtotal),
            HasPriceChanges: items.Any(i => i.PriceChanged),
            CouponCode:      cart.CouponCode,
            DiscountAmount:  cart.DiscountAmount
        );
    }
}
