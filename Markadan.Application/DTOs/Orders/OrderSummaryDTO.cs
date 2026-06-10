namespace Markadan.Application.DTOs.Orders;

public record OrderSummaryDTO(
    int Id,
    string OrderNumber,
    string Status,
    DateTime OrderedAtUtc,
    decimal Total,
    int ItemCount
);
