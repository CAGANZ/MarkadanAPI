namespace Markadan.Application.DTOs.Orders;

public record AdminOrderSummaryDTO(
    int Id,
    string OrderNumber,
    string Status,
    DateTime OrderedAtUtc,
    decimal Total,
    int ItemCount,
    int UserId,
    string UserEmail
);
