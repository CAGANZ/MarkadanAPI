namespace Markadan.Application.DTOs.Products
{
    public record ProductCreateDTO(
        string Title,
        string? Description,
        decimal Price,
        int Stock,          // admin ekranı için
        string? ImageUrl,
        int BrandId,
        int CategoryId
    );
}
