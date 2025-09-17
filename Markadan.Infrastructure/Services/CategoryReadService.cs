using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Categories;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services
{
    public class CategoryReadService : ICategoryReadService
    {
        private readonly MarkadanDbContext _db;

        public CategoryReadService(MarkadanDbContext db)
        {
            ArgumentNullException.ThrowIfNull(db);
            _db = db;
        }

        public async Task<IReadOnlyList<CategoryDTO>> GetAllAsync(CancellationToken ct = default)
        {
            var categoryListResult = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDTO(c.Id, c.Name, c.Description, c.ImageUrl))
                .ToListAsync(ct);
            return categoryListResult;
        }

        public async Task<CategoryDTO?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var categoryResult = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDTO(c.Id, c.Name, c.Description, c.ImageUrl))
            .SingleOrDefaultAsync(ct);
            return categoryResult;
        }
    }
}
