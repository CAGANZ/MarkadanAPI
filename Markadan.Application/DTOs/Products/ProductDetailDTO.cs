namespace Markadan.Application.DTOs.Products
{
    public record ProductDetailDTO(
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
        IReadOnlyList<ProductOptionDTO> Options,
        IReadOnlyList<ProductVariantDTO> Variants
    );
}
