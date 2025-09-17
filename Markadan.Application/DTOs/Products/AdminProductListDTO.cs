namespace Markadan.Application.DTOs.Products;

public record AdminProductListDTO(
    int Id,
    string Title,
    decimal Price,
    string? ImageUrl,
    int BrandId,
    string BrandName,
    int CategoryId,
    string CategoryName,
    int Stock
);
