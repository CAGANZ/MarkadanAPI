namespace Markadan.Application.DTOs.Carts;

public record CartDTO(
    int Id,
    string Status,
    IReadOnlyList<CartItemDTO> Items,
    decimal Total,
    bool HasPriceChanges    // herhangi bir item'ın fiyatı değiştiyse true
);
