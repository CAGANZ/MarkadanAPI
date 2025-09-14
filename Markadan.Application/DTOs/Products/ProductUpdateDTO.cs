namespace Markadan.Application.DTOs.Products
{
    public record ProductUpdateDTO(
    int Id,
    string? Title = null,
    string? Description = null,
    decimal? Price = null,
    int? Stock = null,
    string? ImageUrl = null,
    int? BrandId = null,
    int? CategoryId = null
);
}
