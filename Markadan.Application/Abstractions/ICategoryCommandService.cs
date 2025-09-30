using Markadan.Application.DTOs.Categories;

namespace Markadan.Application.Abstractions
{
    public interface ICategoryCommandService
    {
        Task<CategoryDTO> CreateAsync(CategoryCreateDTO dto, CancellationToken ct = default);
        Task<CategoryDTO?> UpdateAsync(CategoryUpdateDTO dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    }
}
