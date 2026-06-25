namespace Markadan.Application.DTOs.Products;

public record AdminProductExportRow(
    string Title,
    string? Description,
    decimal Price,
    int Stock,
    string? ImageUrl,
    string BrandName,
    string CategoryName
);
