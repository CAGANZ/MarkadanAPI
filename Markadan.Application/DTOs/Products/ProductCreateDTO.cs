namespace Markadan.Application.DTOs.Products
{
    public record ProductCreateDTO(
        string Title,
        string? Description,
        decimal Price,
        int Stock,// sadece adminde gösteceğim
        string? ImageUrl,
        int BrandId,
        int CategoryId
    );
}
