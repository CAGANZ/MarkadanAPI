using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Carts;
using Markadan.Application.DTOs.Coupons;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class CouponService : ICouponService
{
    private readonly MarkadanDbContext _db;

    public CouponService(MarkadanDbContext db) => _db = db;

    // ── Kullanıcı uçları ─────────────────────────────────────────────────────

    public async Task<CartDTO> ApplyCouponAsync(int userId, string code, CancellationToken ct = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Where(c => c.AppUserId == userId && c.Status == CartStatus.Active)
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("Aktif sepet bulunamadı.");

        if (!cart.Items.Any())
            throw new BusinessRuleException("Boş sepete kupon uygulanamaz.");

        var coupon = await _db.Coupons
            .Where(c => c.Code == normalizedCode && c.IsActive)
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("Kupon kodu geçersiz veya aktif değil.");

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
            throw new BusinessRuleException("Kupon süresi dolmuş.");

        if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
            throw new BusinessRuleException("Kupon kullanım limitine ulaşılmış.");

        var grossTotal = cart.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity);

        if (coupon.MinOrderAmount.HasValue && grossTotal < coupon.MinOrderAmount.Value)
            throw new BusinessRuleException(
                $"Bu kupon için minimum sipariş tutarı {coupon.MinOrderAmount:N2} TL'dir.");

        var discount = CalculateDiscount(coupon, grossTotal);

        cart.CouponId       = coupon.Id;
        cart.CouponCode     = coupon.Code;
        cart.DiscountAmount = discount;
        cart.UpdatedAt      = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return BuildCartDTO(cart);
    }

    public async Task<CartDTO> RemoveCouponAsync(int userId, CancellationToken ct = default)
    {
        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Where(c => c.AppUserId == userId && c.Status == CartStatus.Active)
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("Aktif sepet bulunamadı.");

        cart.CouponId       = null;
        cart.CouponCode     = null;
        cart.DiscountAmount = 0;
        cart.UpdatedAt      = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return BuildCartDTO(cart);
    }

    // ── Admin uçları ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CouponDTO>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Coupons
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => ToDTO(c))
            .ToListAsync(ct);
    }

    public async Task<CouponDTO> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var coupon = await _db.Coupons
            .AsNoTracking()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Kupon bulunamadı.");

        return ToDTO(coupon);
    }

    public async Task<CouponDTO> CreateAsync(CreateCouponDTO dto, CancellationToken ct = default)
    {
        if (!Enum.TryParse<CouponType>(dto.Type, ignoreCase: true, out var couponType))
            throw new BusinessRuleException($"Geçersiz kupon türü: '{dto.Type}'. Geçerli değerler: Percentage, FixedAmount.");

        if (couponType == CouponType.Percentage && dto.Value > 100)
            throw new BusinessRuleException("Yüzde indirim 100'den büyük olamaz.");

        var normalizedCode = dto.Code.Trim().ToUpperInvariant();

        var exists = await _db.Coupons.AnyAsync(c => c.Code == normalizedCode, ct);
        if (exists)
            throw new BusinessRuleException($"'{normalizedCode}' kodu zaten kullanımda.");

        var coupon = new Coupon
        {
            Code           = normalizedCode,
            Description    = dto.Description,
            Type           = couponType,
            Value          = dto.Value,
            MinOrderAmount = dto.MinOrderAmount,
            UsageLimit     = dto.UsageLimit,
            ExpiresAt      = dto.ExpiresAt,
            IsActive       = dto.IsActive,
            CreatedAt      = DateTime.UtcNow
        };

        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync(ct);

        return ToDTO(coupon);
    }

    public async Task<CouponDTO> UpdateAsync(int id, UpdateCouponDTO dto, CancellationToken ct = default)
    {
        var coupon = await _db.Coupons
            .AsTracking()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Kupon bulunamadı.");

        if (dto.Description is not null) coupon.Description    = dto.Description;
        if (dto.Value.HasValue)          coupon.Value          = dto.Value.Value;
        if (dto.MinOrderAmount.HasValue) coupon.MinOrderAmount = dto.MinOrderAmount.Value;
        if (dto.UsageLimit.HasValue)     coupon.UsageLimit     = dto.UsageLimit.Value;
        if (dto.ExpiresAt.HasValue)      coupon.ExpiresAt      = dto.ExpiresAt.Value;
        if (dto.IsActive.HasValue)       coupon.IsActive       = dto.IsActive.Value;

        await _db.SaveChangesAsync(ct);

        return ToDTO(coupon);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _db.Coupons
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
            throw new KeyNotFoundException("Kupon bulunamadı.");
    }

    // ── Yardımcı metodlar ────────────────────────────────────────────────────

    internal static decimal CalculateDiscount(Coupon coupon, decimal grossTotal)
    {
        return coupon.Type switch
        {
            CouponType.Percentage  => Math.Round(grossTotal * coupon.Value / 100, 2),
            CouponType.FixedAmount => Math.Min(coupon.Value, grossTotal),
            _                      => 0
        };
    }

    private static CartDTO BuildCartDTO(Cart cart)
    {
        var items = cart.Items.Select(i =>
        {
            var priceChanged = i.UnitPriceSnapshot != i.Product.Price;
            return new CartItemDTO(
                Id:                i.Id,
                ProductId:         i.ProductId,
                Title:             i.Product.Title,
                ImageUrl:          i.Product.ImageUrl,
                UnitPriceSnapshot: i.UnitPriceSnapshot,
                CurrentPrice:      i.Product.Price,
                PriceChanged:      priceChanged,
                Quantity:          i.Quantity,
                Subtotal:          i.UnitPriceSnapshot * i.Quantity
            );
        }).ToList();

        return new CartDTO(
            Id:             cart.Id,
            Status:         cart.Status.ToString(),
            Items:          items,
            Total:          items.Sum(i => i.Subtotal),
            HasPriceChanges: items.Any(i => i.PriceChanged),
            CouponCode:     cart.CouponCode,
            DiscountAmount: cart.DiscountAmount
        );
    }

    private static CouponDTO ToDTO(Coupon c) => new(
        c.Id, c.Code, c.Description, c.Type.ToString(),
        c.Value, c.MinOrderAmount, c.UsageLimit, c.UsageCount,
        c.ExpiresAt, c.IsActive, c.CreatedAt
    );
}
