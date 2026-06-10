using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Products;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services
{
    public class ProductCommandService : IProductCommandService
    {
        private readonly MarkadanDbContext _db;        

        public ProductCommandService(MarkadanDbContext db)
        {
            _db = db;
        }

        public async Task<ProductDetailDTO> CreateAsync(ProductCreateDTO dto, CancellationToken ct = default)
        {
            if (!await _db.Brands.AnyAsync(b => b.Id == dto.BrandId, ct))
                throw new KeyNotFoundException($"Brand {dto.BrandId} not found.");

            if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct))
                throw new KeyNotFoundException($"Category {dto.CategoryId} not found.");

            var entity = new Product
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                ImageUrl = dto.ImageUrl,
                BrandId = dto.BrandId,
                CategoryId = dto.CategoryId,
            };

            _db.Products.Add(entity);
            await _db.SaveChangesAsync(ct);

            return await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == entity.Id)
            .Select(p => new ProductDetailDTO(
                p.Id,
                p.Title,
                p.Price,
                p.ImageUrl,
                p.BrandId,
                p.Brand!.Name,
                p.CategoryId,
                p.Category!.Name,
                p.Description
            ))
            .SingleAsync(ct);
        }

        public async Task<ProductDetailDTO?> UpdateAsync(ProductUpdateDTO dto, CancellationToken ct = default)
        {            
            var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
            if (entity is null) return null;
         
            if (dto.Title is not null)
            {
                var t = dto.Title.Trim();
                if (t.Length == 0) throw new ArgumentException("Title cannot be empty.");
                entity.Title = t;
            }

            if (dto.Description is not null)
                entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (dto.Price is not null)
                entity.Price = dto.Price.Value;

            if (dto.Stock is not null)
                entity.Stock = dto.Stock.Value;

            if (dto.ImageUrl is not null)
                entity.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();

            if (dto.BrandId is not null)
            {
                if (!await _db.Brands.AnyAsync(b => b.Id == dto.BrandId.Value, ct))
                    throw new KeyNotFoundException($"Brand {dto.BrandId} not found.");
                entity.BrandId = dto.BrandId.Value;
            }

            if (dto.CategoryId is not null)
            {
                if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value, ct))
                    throw new KeyNotFoundException($"Category {dto.CategoryId} not found.");
                entity.CategoryId = dto.CategoryId.Value;
            }

            await _db.SaveChangesAsync(ct);
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == entity.Id)
                .Select(p => new ProductDetailDTO(
                    p.Id,
                    p.Title,
                    p.Price,
                    p.ImageUrl,
                    p.BrandId,
                    p.Brand!.Name,
                    p.CategoryId,
                    p.Category!.Name,
                    p.Description
                ))
                .SingleAsync(ct);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var exists = await _db.Products.AnyAsync(p => p.Id == id, ct);
            if (!exists) return false;

            await _db.Products.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
            return true;
        }


    }
}
