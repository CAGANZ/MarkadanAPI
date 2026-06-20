using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Coupons;

public record UpdateCouponDTO(
    [MaxLength(200)] string? Description,
    [Range(0.01, 100000)] decimal? Value,
    [Range(0.01, double.MaxValue)] decimal? MinOrderAmount,
    [Range(1, int.MaxValue)] int? UsageLimit,
    DateTime? ExpiresAt,
    bool? IsActive
);
