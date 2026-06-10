using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Carts;

public record UpdateCartItemQuantityDTO
{
    [Required, Range(0, 1000)]  // 0 → satırı sil
    public int Quantity { get; init; }
}
