using Markadan.Application.DTOs.Products;

namespace Markadan.Application.Abstractions
{
    public interface IProductCommandService
    {
        Task<ProductDetailDTO> CreateAsync(ProductCreateDTO dto, CancellationToken ct = default);

        Task<ProductDetailDTO?> UpdateAsync(ProductUpdateDTO dto, CancellationToken ct = default);

        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
