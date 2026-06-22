using System.Globalization;
using System.Text;
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

            var baseSlug = string.IsNullOrWhiteSpace(dto.Slug)
                ? GenerateSlug(dto.Title)
                : GenerateSlug(dto.Slug);
            var slug = await EnsureUniqueSlugAsync(baseSlug, excludeId: null, ct);

            var entity = new Product
            {
                Title = dto.Title,
                Slug = slug,
                MetaDescription = dto.MetaDescription,
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
                p.Slug,
                p.MetaDescription,
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

            if (dto.Slug is not null)
            {
                var baseSlug = GenerateSlug(dto.Slug);
                entity.Slug = await EnsureUniqueSlugAsync(baseSlug, excludeId: entity.Id, ct);
            }
            else if (dto.Title is not null)
            {
                // Title değişince slug otomatik güncellenmez — admin açıkça slug göndermeli
            }

            if (dto.MetaDescription is not null)
                entity.MetaDescription = string.IsNullOrWhiteSpace(dto.MetaDescription) ? null : dto.MetaDescription.Trim();

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

            if (stockRestored)
                _ = NotifyAndClearStockSubscribersAsync(entity, ct);

            return await _db.Products
                .AsNoTracking()
                .Where(p => p.Id == entity.Id)
                .Select(p => new ProductDetailDTO(
                    p.Id,
                    p.Title,
                    p.Slug,
                    p.MetaDescription,
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

            // Mevcut slug'ları çek — batch içinde + DB'deki çakışmaları önlemek için
            var existingSlugs = new HashSet<string>(
                await _db.Products.AsNoTracking().Select(p => p.Slug).ToListAsync(ct),
                StringComparer.OrdinalIgnoreCase);

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
                if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price < 0)
                                                           { errors.Add(new(rowNumber, $"Geçersiz fiyat: {priceStr}")); continue; }
                if (!int.TryParse(stockStr, out var stock) || stock < 0)
                                                           { errors.Add(new(rowNumber, $"Geçersiz stok: {stockStr}")); continue; }
                if (!brands.TryGetValue(brandName.ToUpperInvariant(), out var brandId))
                                                           { errors.Add(new(rowNumber, $"Marka bulunamadı: {brandName}")); continue; }
                if (!categories.TryGetValue(catName.ToUpperInvariant(), out var categoryId))
                                                           { errors.Add(new(rowNumber, $"Kategori bulunamadı: {catName}")); continue; }

                var baseSlug = GenerateSlug(title);
                var slug = baseSlug;
                int suffix = 2;
                while (existingSlugs.Contains(slug)) slug = $"{baseSlug}-{suffix++}";
                existingSlugs.Add(slug);

                toInsert.Add(new Product
                {
                    Title       = title,
                    Slug        = slug,
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

        private static string GenerateSlug(string input)
        {
            // Türkçe karakter dönüşümü
            var s = input
                .Replace('ı', 'i').Replace('İ', 'i')
                .Replace('ğ', 'g').Replace('Ğ', 'g')
                .Replace('ü', 'u').Replace('Ü', 'u')
                .Replace('ş', 's').Replace('Ş', 's')
                .Replace('ö', 'o').Replace('Ö', 'o')
                .Replace('ç', 'c').Replace('Ç', 'c');

            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                char lower = char.ToLowerInvariant(c);
                if (char.IsLetterOrDigit(lower))
                    sb.Append(lower);
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-')
                    sb.Append('-');
            }
            while (sb.Length > 0 && sb[sb.Length - 1] == '-') sb.Length--;
            var result = sb.Length > 0 ? sb.ToString() : "urun";
            return result.Length > 200 ? result[..200] : result;
        }

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, int? excludeId, CancellationToken ct)
        {
            var slug = baseSlug;
            int suffix = 2;
            while (true)
            {
                var exists = await _db.Products.AnyAsync(
                    p => p.Slug == slug && (excludeId == null || p.Id != excludeId), ct);
                if (!exists) return slug;
                slug = $"{baseSlug}-{suffix++}";
            }
        }

        public async Task SubscribeStockNotificationAsync(int productId, string email, string? name, CancellationToken ct = default)
        {
            bool productExists = await _db.Products.AnyAsync(p => p.Id == productId, ct);
            if (!productExists) throw new KeyNotFoundException($"Product {productId} not found.");

            bool alreadySubscribed = await _db.StockNotifications
                .AnyAsync(n => n.ProductId == productId && n.Email == email, ct);
            if (alreadySubscribed) return;

            _db.StockNotifications.Add(new Domain.Models.StockNotification
            {
                ProductId = productId,
                Email     = email,
                Name      = name,
            });
            await _db.SaveChangesAsync(ct);
        }

        private async Task NotifyAndClearStockSubscribersAsync(Product product, CancellationToken ct)
        {
            var subscribers = await _db.StockNotifications
                .Where(n => n.ProductId == product.Id)
                .ToListAsync(ct);

            if (subscribers.Count == 0) return;

            foreach (var sub in subscribers)
            {
                try
                {
                    string displayName = sub.Name ?? sub.Email;
                    string body = $"<p>Merhaba {displayName},</p>" +
                                  $"<p>Stok bildirimi almak istediğiniz <strong>{product.Title}</strong> ürünü tekrar stokta!</p>" +
                                  $"<p>Hemen sipariş vermek için mağazamızı ziyaret edin.</p>";
                    await _email.SendAsync(sub.Email, displayName, $"Stok geldi: {product.Title}", body, ct);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Stok bildirimi gönderilemedi: {Email}", sub.Email);
                }
            }

            _db.StockNotifications.RemoveRange(subscribers);
            await _db.SaveChangesAsync(ct);
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
