using Markadan.Application.DTOs.Common;
using Markadan.Application.DTOs.Products;

namespace Markadan.Application.Abstractions
{
    public interface IProductReadService
    {
        Task<ProductDetailDTO?> GetDetailAsync(int id, CancellationToken ct = default);

        Task<PagedResult<ProductListDTO>> ListAsync(
        int? categoryId,
        int? brandId,
        string? q,
        decimal? min,
        decimal? max,
        string? sort,
        int page = 1,
        int pageSize = 12,
        CancellationToken ct = default);
    }
}
