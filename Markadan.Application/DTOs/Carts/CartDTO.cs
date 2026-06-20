namespace Markadan.Application.DTOs.Carts;

public record CartDTO(
    int Id,
    string Status,
    IReadOnlyList<CartItemDTO> Items,
    decimal Total,             // ürünler toplamı (indirim öncesi)
    bool HasPriceChanges,      // herhangi bir item'ın fiyatı değiştiyse true
    string? CouponCode = null,
    decimal DiscountAmount = 0
)
{
    public decimal FinalTotal => Total - DiscountAmount;
};
