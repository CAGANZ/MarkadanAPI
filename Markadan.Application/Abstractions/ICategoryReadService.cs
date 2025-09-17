using Markadan.Application.DTOs.Categories;

namespace Markadan.Application.Abstractions
{
    public interface ICategoryReadService
    {
        Task<IReadOnlyList<CategoryDTO>> GetAllAsync(CancellationToken ct = default);
        
        Task<CategoryDTO?> GetByIdAsync(int id, CancellationToken ct =default);
    }
}
