namespace Markadan.Application.DTOs.Products;

public record AdminProductDetailDTO(
    int Id,
    string Title,
    decimal Price,
    string? ImageUrl,
    int BrandId,
    string BrandName,
    int CategoryId,
    string CategoryName,
    string? Description,
    int Stock
);
