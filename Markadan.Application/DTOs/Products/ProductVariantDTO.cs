namespace Markadan.Application.DTOs.Products
{
    // Vitrin için varyant — Price etkin fiyattır (varyantın özel fiyatı yoksa ürün fiyatı)
    public record ProductVariantDTO(
        int Id,
        string? Sku,
        decimal Price,
        int Stock,
        string? ImageUrl,
        IReadOnlyList<int> OptionValueIds
    );

    public record ProductOptionDTO(
        int Id,
        string Name,
        IReadOnlyList<ProductOptionValueDTO> Values
    );

    public record ProductOptionValueDTO(
        int Id,
        string Value
    );
}
