namespace Markadan.Application.DTOs.Carts
{
    public record CartDTO(int Id, IReadOnlyList<CartItemDTO> Items, decimal Total);
}
