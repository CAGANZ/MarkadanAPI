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

        public async Task<BulkUploadResultDTO> BulkUploadAsync(Stream csvStream, CancellationToken ct = default)
        {
            // Tüm brand/kategori isim→id eşlemesini tek sorguda çek
            var brands     = await _db.Brands.AsNoTracking().ToDictionaryAsync(b => b.Name.ToUpperInvariant(), b => b.Id, ct);
            var categories = await _db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Name.ToUpperInvariant(), c => c.Id, ct);

            var errors    = new List<BulkUploadErrorDTO>();
            var toInsert  = new List<Product>();
            int rowNumber = 1; // başlık satırı

            using var reader = new StreamReader(csvStream);
            var header = await reader.ReadLineAsync(ct); // başlık atla

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                rowNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Basit CSV parse — tırnaklı alan desteği yok; değerlerde virgül geçmemeli
                var cols = line.Split(',');
                if (cols.Length < 7)
                {
                    errors.Add(new(rowNumber, "Eksik sütun — 7 sütun bekleniyor: Title,Description,Price,Stock,ImageUrl,BrandName,CategoryName"));
                    continue;
                }

                var title       = cols[0].Trim();
                var description = cols[1].Trim();
                var priceStr    = cols[2].Trim();
                var stockStr    = cols[3].Trim();
                var imageUrl    = cols[4].Trim();
                var brandName   = cols[5].Trim();
                var catName     = cols[6].Trim();

                if (string.IsNullOrEmpty(title))          { errors.Add(new(rowNumber, "Title boş olamaz")); continue; }
                if (!decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price) || price < 0)
                                                           { errors.Add(new(rowNumber, $"Geçersiz fiyat: {priceStr}")); continue; }
                if (!int.TryParse(stockStr, out var stock) || stock < 0)
                                                           { errors.Add(new(rowNumber, $"Geçersiz stok: {stockStr}")); continue; }
                if (!brands.TryGetValue(brandName.ToUpperInvariant(), out var brandId))
                                                           { errors.Add(new(rowNumber, $"Marka bulunamadı: {brandName}")); continue; }
                if (!categories.TryGetValue(catName.ToUpperInvariant(), out var categoryId))
                                                           { errors.Add(new(rowNumber, $"Kategori bulunamadı: {catName}")); continue; }

                toInsert.Add(new Product
                {
                    Title       = title,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description,
                    Price       = price,
                    Stock       = stock,
                    ImageUrl    = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl,
                    BrandId     = brandId,
                    CategoryId  = categoryId,
                });
            }

            if (toInsert.Count > 0)
            {
                _db.Products.AddRange(toInsert);
                await _db.SaveChangesAsync(ct);
            }

            return new BulkUploadResultDTO(toInsert.Count, errors.Count, errors);
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
