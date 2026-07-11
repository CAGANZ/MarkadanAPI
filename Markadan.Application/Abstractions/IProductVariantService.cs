using Markadan.Application.DTOs.Products.Variants;

namespace Markadan.Application.Abstractions;

// Admin: ürün opsiyon (eksen) ve varyant yönetimi
public interface IProductVariantService
{
    // Opsiyonlar
    Task<IReadOnlyList<AdminProductOptionDTO>> GetOptionsAsync(int productId, CancellationToken ct = default);
    Task<AdminProductOptionDTO> AddOptionAsync(int productId, CreateOptionDTO dto, CancellationToken ct = default);
    Task<bool> DeleteOptionAsync(int productId, int optionId, CancellationToken ct = default);
    Task<AdminOptionValueDTO> AddOptionValueAsync(int productId, int optionId, CreateOptionValueDTO dto, CancellationToken ct = default);
    Task<bool> DeleteOptionValueAsync(int productId, int valueId, CancellationToken ct = default);

    // Varyantlar
    Task<IReadOnlyList<AdminVariantDTO>> GetVariantsAsync(int productId, CancellationToken ct = default);
    Task<AdminVariantDTO> AddVariantAsync(int productId, CreateVariantDTO dto, CancellationToken ct = default);
    Task<AdminVariantDTO?> UpdateVariantAsync(int productId, int variantId, UpdateVariantDTO dto, CancellationToken ct = default);
    Task<bool> DeleteVariantAsync(int productId, int variantId, CancellationToken ct = default);
}
