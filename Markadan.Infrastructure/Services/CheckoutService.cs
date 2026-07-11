using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Carts;
using Markadan.Application.DTOs.Orders;
using Markadan.Application.DTOs.Payment;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Markadan.Infrastructure.Services;

public sealed class CheckoutService : ICheckoutService
{
    private readonly MarkadanDbContext _db;
    private readonly IEmailService _email;
    private readonly IPaymentService _payment;
    private readonly IWhatsAppService _whatsApp;
    private readonly ILogger<CheckoutService> _log;
    private readonly int _lowStockThreshold;
    private readonly string? _adminEmail;

    public CheckoutService(
        MarkadanDbContext db,
        IEmailService email,
        IPaymentService payment,
        IWhatsAppService whatsApp,
        ILogger<CheckoutService> log,
        IConfiguration config)
    {
        _db                = db;
        _email             = email;
        _payment           = payment;
        _whatsApp          = whatsApp;
        _log               = log;
        _lowStockThreshold = config.GetValue<int>("Inventory:LowStockThreshold", 5);
        _adminEmail        = config["Inventory:AdminEmail"];
    }

    // ── iyzico akışı ────────────────────────────────────────────────────────────

    public async Task<InitiateCheckoutResponseDTO> InitiatePaymentAsync(
        int userId, int addressId, string userIp, CancellationToken ct = default)
    {
        var cart = await LoadActiveCartAsync(userId, ct);
        ValidateCartForCheckout(cart);

        var address = await GetAddressAsync(userId, addressId, ct);
        var user    = cart.AppUser;

        // Daha önce başlatılmış ama tamamlanmamış ödeme varsa üzerine yaz
        cart.Status               = CartStatus.PaymentPending;
        cart.ShippingAddressId    = address.Id;
        cart.ShippingStreet       = address.Street;
        cart.ShippingCity         = address.City;
        cart.ShippingState        = address.State;
        cart.ShippingPostalCode   = address.PostalCode;
        cart.ShippingCountry      = address.Country;
        cart.UpdatedAt            = DateTime.UtcNow;

        var conversationId = $"cart-{cart.Id}";
        cart.IyzicoConversationId = conversationId;
        await _db.SaveChangesAsync(ct);

        var grossTotal  = cart.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity);
        var finalTotal  = Math.Max(0, grossTotal - cart.DiscountAmount);

        // Kupon indirimi basket item'larına orantılı dağıtılır (iyzico toplamla eşleşmeli)
        var basketItems = BuildBasketItems(cart.Items.ToList(), grossTotal, finalTotal);

        var initiateReq = new PaymentInitiateRequest(
            ConversationId:     conversationId,
            TotalAmount:        finalTotal,
            UserId:             user.Id,
            UserName:           user.Name,
            UserSurname:        user.Surname,
            UserEmail:          user.Email!,
            UserIp:             userIp,
            ShippingContactName: $"{user.Name} {user.Surname}",
            ShippingCity:       address.City,
            ShippingCountry:    address.Country,
            ShippingAddress:    $"{address.Street}, {address.State}",
            Items:              basketItems
        );

        var result = await _payment.InitiateAsync(initiateReq, ct);
        if (!result.Success)
            throw new BusinessRuleException(result.Error ?? "Ödeme başlatılamadı.");

        return new InitiateCheckoutResponseDTO(conversationId, result.Token!, result.CheckoutFormContent!);
    }

    public async Task<OrderDTO> ConfirmPaymentAsync(string token, CancellationToken ct = default)
    {
        var confirmResult = await _payment.ConfirmAsync(token, ct);
        if (!confirmResult.Success)
            throw new BusinessRuleException(confirmResult.Error ?? "Ödeme doğrulanamadı.");

        // conversationId = "cart-{cartId}" formatında
        var cartId = ParseCartIdFromConversationId(confirmResult.ConversationId!);

        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.AppUser)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.SelectedValues)
                        .ThenInclude(sv => sv.OptionValue)
                            .ThenInclude(ov => ov.Option)
            .Where(c => c.Id == cartId && c.Status == CartStatus.PaymentPending)
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("Ödeme beklenen sipariş bulunamadı.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            foreach (var item in cart.Items)
            {
                var affected = await DecrementStockAsync(item, ct);

                if (affected == 0)
                    throw new BusinessRuleException($"'{item.Product.Title}' için yeterli stok yok.");
            }

            cart.Status            = CartStatus.Ordered;
            cart.OrderedAtUtc      = DateTime.UtcNow;
            cart.UpdatedAt         = DateTime.UtcNow;
            cart.OrderNumber       = GenerateOrderNumber();
            cart.IyzicoPaymentId   = confirmResult.PaymentId;
            cart.PaidAtUtc         = DateTime.UtcNow;

            // Kupon kullanım sayısını artır
            if (cart.CouponId.HasValue)
                await _db.Coupons
                    .Where(c => c.Id == cart.CouponId.Value)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsageCount, c => c.UsageCount + 1), ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        _ = NotifyLowStockAsync(cart.Items.Select(i => i.ProductId).ToList(), CancellationToken.None);
        _ = SendOrderWhatsAppAsync(cart, CancellationToken.None);

        return BuildOrderDTO(cart);
    }

    // ── Eski akış (nakit/kapıda ödeme — ileride kullanılabilir) ─────────────────

    public async Task<CartDTO> CheckoutAsync(int userId, int addressId, CancellationToken ct = default)
    {
        var cart    = await LoadActiveCartAsync(userId, ct);
        ValidateCartForCheckout(cart);
        var address = await GetAddressAsync(userId, addressId, ct);

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            foreach (var item in cart.Items)
            {
                var affected = await DecrementStockAsync(item, ct);

                if (affected == 0)
                    throw new BusinessRuleException($"'{item.Product.Title}' için yeterli stok yok.");
            }

            cart.Status             = CartStatus.Ordered;
            cart.OrderedAtUtc       = DateTime.UtcNow;
            cart.UpdatedAt          = DateTime.UtcNow;
            cart.OrderNumber        = GenerateOrderNumber();
            cart.ShippingAddressId  = address.Id;
            cart.ShippingStreet     = address.Street;
            cart.ShippingCity       = address.City;
            cart.ShippingState      = address.State;
            cart.ShippingPostalCode = address.PostalCode;
            cart.ShippingCountry    = address.Country;

            if (cart.CouponId.HasValue)
                await _db.Coupons
                    .Where(c => c.Id == cart.CouponId.Value)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsageCount, c => c.UsageCount + 1), ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        _ = NotifyLowStockAsync(cart.Items.Select(i => i.ProductId).ToList(), CancellationToken.None);
        _ = SendOrderWhatsAppAsync(cart, CancellationToken.None);

        var items = cart.Items.Select(i => new CartItemDTO(
            Id: i.Id, ProductId: i.ProductId, Title: i.Product.Title,
            ImageUrl: i.ProductVariant?.ImageUrl ?? i.Product.ImageUrl,
            UnitPriceSnapshot: i.UnitPriceSnapshot,
            CurrentPrice: EffectiveCurrentPrice(i), PriceChanged: false,
            Quantity: i.Quantity, Subtotal: i.UnitPriceSnapshot * i.Quantity,
            VariantId: i.ProductVariantId, VariantLabel: VariantLabel(i)
        )).ToList();

        return new CartDTO(cart.Id, cart.Status.ToString(), items, items.Sum(i => i.Subtotal), false);
    }

    // ── Yardımcı metodlar ────────────────────────────────────────────────────────

    private async Task<Cart> LoadActiveCartAsync(int userId, CancellationToken ct)
    {
        return await _db.Carts
            .AsTracking()
            .Include(c => c.AppUser)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.SelectedValues)
                        .ThenInclude(sv => sv.OptionValue)
                            .ThenInclude(ov => ov.Option)
            .Where(c => c.AppUserId == userId &&
                        (c.Status == CartStatus.Active || c.Status == CartStatus.PaymentPending))
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("Aktif sepet bulunamadı.");
    }

    // Stok düşme — varyantlı satırda ProductVariants, basit satırda Products tablosuna koşullu UPDATE
    private async Task<int> DecrementStockAsync(CartItem item, CancellationToken ct)
    {
        if (item.ProductVariantId is not null)
        {
            return await _db.ProductVariants
                .Where(v => v.Id == item.ProductVariantId.Value && v.Stock >= item.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock - item.Quantity), ct);
        }

        return await _db.Products
            .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity), ct);
    }

    // Bir sepet satırının etkin güncel fiyatı (varyant fiyatı yoksa ürün fiyatı)
    private static decimal EffectiveCurrentPrice(CartItem item)
        => item.ProductVariant is not null
            ? (item.ProductVariant.Price ?? item.Product.Price)
            : item.Product.Price;

    // Bir sepet satırının varyant etiketi ("Kırmızı / M") — yoksa null
    private static string? VariantLabel(CartItem item)
    {
        if (item.ProductVariant is null || item.ProductVariant.SelectedValues.Count == 0)
            return null;
        var values = item.ProductVariant.SelectedValues
            .OrderBy(sv => sv.OptionValue.Option.SortOrder)
            .Select(sv => sv.OptionValue.Value);
        return string.Join(" / ", values);
    }

    private static void ValidateCartForCheckout(Cart cart)
    {
        if (!cart.Items.Any())
            throw new BusinessRuleException("Sepet boş.");

        var changedItems = cart.Items
            .Where(i => i.UnitPriceSnapshot != EffectiveCurrentPrice(i))
            .Select(i => i.Product.Title)
            .ToList();

        if (changedItems.Count > 0)
            throw new BusinessRuleException(
                $"Bazı ürünlerin fiyatı değişti: {string.Join(", ", changedItems)}. " +
                "Güncel fiyatları görmek için sepeti yenileyiniz.");
    }

    private async Task<Address> GetAddressAsync(int userId, int addressId, CancellationToken ct)
    {
        return await _db.Addresses
            .Where(a => a.Id == addressId && a.AppUserId == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Adres bulunamadı.");
    }

    private static int ParseCartIdFromConversationId(string conversationId)
    {
        // Format: "cart-{cartId}"
        var parts = conversationId.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[1], out var id))
            return id;
        throw new BusinessRuleException($"Geçersiz conversationId: {conversationId}");
    }

    private static OrderDTO BuildOrderDTO(Cart cart)
    {
        var items = cart.Items.Select(i => new OrderItemDTO(
            i.ProductId, i.Product.Title,
            i.ProductVariant?.ImageUrl ?? i.Product.ImageUrl,
            i.UnitPriceSnapshot, i.Quantity, i.UnitPriceSnapshot * i.Quantity,
            i.ProductVariantId, VariantLabel(i)
        )).ToList();

        return new OrderDTO(
            cart.Id, cart.OrderNumber!, cart.Status.ToString(), cart.OrderedAtUtc!.Value,
            items.Sum(i => i.Subtotal), items,
            cart.ShippingStreet, cart.ShippingCity, cart.ShippingState,
            cart.ShippingPostalCode, cart.ShippingCountry
        );
    }

    private async Task SendOrderWhatsAppAsync(Cart cart, CancellationToken ct)
    {
        var phone = cart.AppUser?.PhoneNumber;
        if (string.IsNullOrWhiteSpace(phone)) return;

        var itemLines = string.Join("\n", cart.Items.Select(i =>
            $"• {i.Product.Title} x{i.Quantity} — {i.UnitPriceSnapshot * i.Quantity:F2} TL"));

        var total    = cart.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity);
        var message  =
            $"Merhaba {cart.AppUser!.Name} 👋\n" +
            $"Siparişiniz alındı! 🎉\n\n" +
            $"Sipariş No: {cart.OrderNumber}\n\n" +
            $"{itemLines}\n\n" +
            $"Toplam: {total:F2} TL\n\n" +
            $"Siparişinizi takip etmek için uygulamayı açabilirsiniz.";

        try { await _whatsApp.SendAsync(phone, message, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "WhatsApp bildirimi gönderilemedi"); }
    }

    private async Task NotifyLowStockAsync(List<int> productIds, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_adminEmail)) return;
        try
        {
            var lowStock = await _db.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id) && p.Stock <= _lowStockThreshold)
                .Select(p => new { p.Title, p.Stock })
                .ToListAsync(ct);

            if (lowStock.Count == 0) return;

            var rows = string.Join("", lowStock.Select(p =>
                $"<tr><td>{p.Title}</td><td><strong>{p.Stock} adet</strong></td></tr>"));

            await _email.SendAsync(_adminEmail, "Mağaza Yöneticisi",
                $"Stok uyarısı: {lowStock.Count} üründe kritik seviye",
                $"<p>Stok kritik seviyeye ({_lowStockThreshold}) düştü:</p><table border='1' cellpadding='6' style='border-collapse:collapse'><thead><tr><th>Ürün</th><th>Kalan</th></tr></thead><tbody>{rows}</tbody></table>",
                ct);
        }
        catch (Exception ex) { _log.LogWarning(ex, "Stok uyarısı maili gönderilemedi"); }
    }

    // iyzico: basket item'ların toplamı TotalAmount ile eşleşmeli.
    // Kupon indirimi orantılı dağıtılır; yuvarlama farkı son kaleme eklenir.
    private static List<PaymentBasketItem> BuildBasketItems(
        List<CartItem> items, decimal grossTotal, decimal finalTotal)
    {
        if (grossTotal == 0)
            return items.Select(i => new PaymentBasketItem(
                i.Id.ToString(), i.Product.Title, "Ürün", 0)).ToList();

        var result = items.Select(i =>
        {
            var itemGross   = i.UnitPriceSnapshot * i.Quantity;
            var itemFinal   = Math.Round(finalTotal * itemGross / grossTotal, 2);
            return new PaymentBasketItem(i.Id.ToString(), i.Product.Title, "Ürün", itemFinal);
        }).ToList();

        var diff = finalTotal - result.Sum(x => x.Price);
        if (diff != 0 && result.Count > 0)
            result[^1] = result[^1] with { Price = result[^1].Price + diff };

        return result;
    }

    private static string GenerateOrderNumber()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var sb = new StringBuilder("MRK-", 12);
        foreach (var b in bytes)
            sb.Append(chars[b % chars.Length]);
        return sb.ToString();
    }
}
