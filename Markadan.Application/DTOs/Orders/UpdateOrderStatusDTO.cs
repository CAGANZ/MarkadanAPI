using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Orders;

public record UpdateOrderStatusDTO
{
    [Required]
    public string Status { get; init; } = "";

    [MaxLength(100)]
    public string? TrackingNumber { get; init; }

    [MaxLength(500)]
    public string? TrackingUrl { get; init; }
}
