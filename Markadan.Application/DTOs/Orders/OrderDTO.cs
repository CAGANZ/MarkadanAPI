namespace Markadan.Application.DTOs.Orders;

public record OrderDTO(
    int Id,
    string OrderNumber,
    string Status,
    DateTime OrderedAtUtc,
    decimal Total,
    IReadOnlyList<OrderItemDTO> Items,
    string? ShippingStreet,
    string? ShippingCity,
    string? ShippingState,
    string? ShippingPostalCode,
    string? ShippingCountry
);
