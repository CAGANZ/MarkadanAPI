using Markadan.Application.DTOs.Brands;

namespace Markadan.Application.Abstractions;

public interface IBrandReadService
{
    Task<IReadOnlyList<BrandDTO>> GetAllAsync(CancellationToken ct = default);
    Task<BrandDTO?> GetByIdAsync(int id, CancellationToken ct = default);
}
