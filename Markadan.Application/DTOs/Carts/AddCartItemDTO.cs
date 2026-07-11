using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Carts;

public record AddCartItemDTO
{
    [Required]
    public int ProductId { get; init; }

    // Varyantlı ürünlerde zorunlu, basit ürünlerde gönderilmez
    public int? ProductVariantId { get; init; }

    [Required, Range(1, 1000)]
    public int Quantity { get; init; }
}
