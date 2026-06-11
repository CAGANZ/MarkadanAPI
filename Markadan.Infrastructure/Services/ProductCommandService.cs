using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Products;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Markadan.Infrastructure.Services
{
    public class ProductCommandService : IProductCommandService
    {
        private readonly MarkadanDbContext _db;
        private readonly IEmailService _email;
        private readonly ILogger<ProductCommandService> _log;

        public ProductCommandService(MarkadanDbContext db, IEmailService email, ILogger<ProductCommandService> log)
        {
            _db    = db;
            _email = email;
            _log   = log;
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

            decimal oldPrice = entity.Price;
            int     oldStock = entity.Stock;

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

            // Favori bildirimleri — fire-and-forget (hata gönderimi ürün güncellemeyi engellemesin)
            bool priceDropped   = dto.Price is not null && entity.Price < oldPrice;
            bool stockRestored  = dto.Stock is not null && oldStock <= 0 && entity.Stock > 0;

            if (priceDropped || stockRestored)
                _ = NotifyWishlistUsersAsync(entity, priceDropped, stockRestored, ct);

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

        private async Task NotifyWishlistUsersAsync(Product product, bool priceDropped, bool stockRestored, CancellationToken ct)
        {
            var users = await _db.WishlistItems
                .Where(w => w.ProductId == product.Id)
                .Select(w => new { w.AppUser.Email, w.AppUser.Name })
                .ToListAsync(ct);

            string subject = priceDropped
                ? $"Fiyat düştü: {product.Title}"
                : $"Stok geldi: {product.Title}";

            foreach (var u in users)
            {
                if (string.IsNullOrEmpty(u.Email)) continue;
                try
                {
                    string body = priceDropped
                        ? $"<p>Merhaba {u.Name},</p><p>Favorilerinize eklediğiniz <strong>{product.Title}</strong> ürününün fiyatı <strong>{product.Price:N2} ₺</strong> oldu.</p>"
                        : $"<p>Merhaba {u.Name},</p><p>Favorilerinize eklediğiniz <strong>{product.Title}</strong> tekrar stokta!</p>";

                    await _email.SendAsync(u.Email, u.Name, subject, body, ct);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Favori bildirimi gönderilemedi: {Email}", u.Email);
                }
            }
        }


    }
}
