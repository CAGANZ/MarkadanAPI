namespace Markadan.Application.DTOs.Coupons;

public record CouponDTO(
    int Id,
    string Code,
    string Description,
    string Type,
    decimal Value,
    decimal? MinOrderAmount,
    int? UsageLimit,
    int UsageCount,
    DateTime? ExpiresAt,
    bool IsActive,
    DateTime CreatedAt
);
