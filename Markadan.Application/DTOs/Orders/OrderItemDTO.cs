namespace Markadan.Application.DTOs.Orders;

public record OrderItemDTO(
    int ProductId,
    string Title,
    string? ImageUrl,
    decimal UnitPriceSnapshot,
    int Quantity,
    decimal Subtotal
);
