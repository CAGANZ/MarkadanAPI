using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Carts;

public record CheckoutRequestDTO
{
    [Required]
    public int AddressId { get; init; }
}
