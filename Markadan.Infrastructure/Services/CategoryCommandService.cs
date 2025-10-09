using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Categories;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services
{
    public class CategoryCommandService : ICategoryCommandService
    {
        private readonly MarkadanDbContext _db;

        public CategoryCommandService(MarkadanDbContext db)
        {
            _db = db;
        }

        public async Task<CategoryDTO> CreateAsync(CategoryCreateDTO dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new InvalidOperationException("Name cannot be empty");

            var name = dto.Name.Trim();

            var exists = await _db.Categories.AnyAsync(c => c.Name == name, ct);
            if (exists)
                throw new BusinessRuleException($" Category name '{name}' already exists");

            var entity = new Category()
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim()
            };

            _db.Categories.Add(entity);
            await _db.SaveChangesAsync(ct);
            return new CategoryDTO(entity.Id, entity.Name, entity.Description, entity.ImageUrl);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {           
            var inUse = await _db.Products
                .AsNoTracking()
                .AnyAsync(p => p.CategoryId == id, ct);
            if (inUse)
                throw new BusinessRuleException("Category is in use and cannot be deleted");
            
            var rows = await _db.Categories.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
            if (rows == 0)
                throw new KeyNotFoundException("Category not found");

            return true;
        }


        public async Task<CategoryDTO?> UpdateAsync(CategoryUpdateDTO dto, CancellationToken ct = default)
        {
            var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.Id, ct);
            if (entity is null)
                return null;
            if (dto.Name is not null)
            {
                var name = dto.Name.Trim();
                if (name.Length == 0)
                    throw new InvalidOperationException("Name cannot be empty");
                var exists = await _db.Categories.AnyAsync(c => c.Id != dto.Id && c.Name == name, ct);
                if (exists)
                    throw new BusinessRuleException($" Category {name} already exists");
                entity.Name = name;
            }
            if (dto.Description is not null)
                entity.Description = dto.Description.Trim().Length == 0 ? null : dto.Description.Trim();
            if(dto.ImageUrl is not null)
                entity.ImageUrl = dto.ImageUrl.Trim().Length == 0 ? null : dto.ImageUrl.Trim();
            await _db.SaveChangesAsync(ct);

            return new CategoryDTO(entity.Id, entity.Name, entity.Description, entity.ImageUrl);            
        }
    }
}
