using Markadan.Domain.Models.Enums;

namespace Markadan.Domain.Models;

public class Coupon
{
    public int Id { get; set; }

    public required string Code { get; set; }       // büyük harfe normalize edilir
    public required string Description { get; set; } // admin notları

    public CouponType Type { get; set; }             // Percentage | FixedAmount
    public decimal Value { get; set; }               // %10 → 10 veya 50TL → 50

    public decimal? MinOrderAmount { get; set; }     // minimum sepet tutarı
    public int? UsageLimit { get; set; }             // null = limitsiz
    public int UsageCount { get; set; }              // kaç sipariş kullandı

    public DateTime? ExpiresAt { get; set; }         // null = süresiz
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
