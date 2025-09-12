namespace Markadan.Application.DTOs.Products
{
    public record ProductUpdateDTO(
        int Id,
        string Title,
        string? Description,
        decimal Price,
        int Stock,
        string? ImageUrl,
        int BrandId,
        int CategoryId
    );
}
