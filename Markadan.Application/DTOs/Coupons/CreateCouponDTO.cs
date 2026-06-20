using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Coupons;

public record CreateCouponDTO(
    [Required][MinLength(3)][MaxLength(50)] string Code,
    [Required][MaxLength(200)] string Description,
    [Required] string Type,         // "Percentage" | "FixedAmount"
    [Range(0.01, 100000)] decimal Value,
    [Range(0.01, double.MaxValue)] decimal? MinOrderAmount,
    [Range(1, int.MaxValue)] int? UsageLimit,
    DateTime? ExpiresAt,
    bool IsActive = true
);
