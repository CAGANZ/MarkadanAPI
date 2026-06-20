using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Carts;

public record ApplyCouponDTO(
    [Required][MinLength(1)][MaxLength(50)] string Code
);
