using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Products;

public record StockNotifyRequestDTO(
    [Required, EmailAddress, MaxLength(200)] string Email,
    [MaxLength(100)] string? Name
);
