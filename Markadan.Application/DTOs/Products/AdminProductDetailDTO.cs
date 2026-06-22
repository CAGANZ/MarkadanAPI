namespace Markadan.Application.DTOs.Products;

public record AdminProductDetailDTO(
    int Id,
    string Title,
    string Slug,
    string? MetaDescription,
    decimal Price,
    string? ImageUrl,
    int BrandId,
    string BrandName,
    int CategoryId,
    string CategoryName,
    string? Description,
    int Stock
);
