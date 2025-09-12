namespace Markadan.Application.DTOs.Carts
{
    public record CartItemDTO(
        int ProductId,
        string TitleSnapshot,
        string? ImageUrlSnapshot,
        decimal UnitPriceSnapshot,
        int Quantity,
        decimal Subtotal
    );
}
