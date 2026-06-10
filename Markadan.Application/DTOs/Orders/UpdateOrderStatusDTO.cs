using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Orders;

public record UpdateOrderStatusDTO
{
    [Required]
    public string Status { get; init; } = "";
}
