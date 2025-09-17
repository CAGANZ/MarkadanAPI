using Markadan.Application.DTOs.Brands;


namespace Markadan.Application.Abstractions;

public interface IBrandCommandService
{
    Task<BrandDTO> CreateAsync(BrandCreateDTO dto, CancellationToken ct = default);

    Task<BrandDTO?> UpdateAsync(BrandUpdateDTO dto, CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}