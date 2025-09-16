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
            var brandExits = await _db.Brands.AnyAsync(b => b.Id == dto.BrandId, ct);
            if (!brandExits)
                throw new InvalidOperationException($"Brand {dto.BrandId} not found");

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct);
            if (!categoryExists)
                throw new InvalidOperationException($"Category {dto.CategoryId} not found");

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
            // 1) Ürün var mı?
            var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
            if (entity is null) return null; // controller 404 dönecek

            // 2) Alan bazlı güncelle (yalnız gelenleri uygula)
            if (dto.Title is not null)
            {
                var t = dto.Title.Trim();
                if (t.Length == 0) throw new InvalidOperationException("Title cannot be empty.");
                entity.Title = t;
            }

            if (dto.Description is not null)
                entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (dto.Price is not null)
            {
                if (dto.Price.Value < 0) throw new InvalidOperationException("Price cannot be negative.");
                entity.Price = dto.Price.Value;
            }

            if (dto.Stock is not null)
            {
                if (dto.Stock.Value < 0) throw new InvalidOperationException("Stock cannot be negative.");
                entity.Stock = dto.Stock.Value; // public’e sızdırmıyoruz, admin tarafı
            }

            if (dto.ImageUrl is not null)
                entity.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();

            if (dto.BrandId is not null)
            {
                var exists = await _db.Brands.AnyAsync(b => b.Id == dto.BrandId.Value, ct);
                if (!exists) throw new InvalidOperationException($"Brand {dto.BrandId} not found.");
                entity.BrandId = dto.BrandId.Value;
            }

            if (dto.CategoryId is not null)
            {
                var exists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value, ct);
                if (!exists) throw new InvalidOperationException($"Category {dto.CategoryId} not found.");
                entity.CategoryId = dto.CategoryId.Value;
            }

            await _db.SaveChangesAsync(ct);

            // 3) Güncel detayı projekte edip dön (UI direkt kullanabilsin)
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
            // Yoksa 404’e zemin hazırlıyoruz
            var exists = await _db.Products.AnyAsync(p => p.Id == id, ct);
            if (!exists) return false;

            // Hızlı ve server-side delete (EF Core 9)
            await _db.Products.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
            return true;
        }


    }
}
