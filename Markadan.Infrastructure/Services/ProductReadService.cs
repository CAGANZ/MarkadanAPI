using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Common;
using Markadan.Application.DTOs.Products;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class ProductReadService : IProductReadService
{
    private readonly MarkadanDbContext _db;

    public ProductReadService(MarkadanDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AdminProductListDTO>> ListAdminAsync(
    int? categoryId, int? brandId, string? q, decimal? min, decimal? max,
    string? sort, int page = 1, int pageSize = 12, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 12;

        var query = _db.Products.Include(p => p.Brand).Include(p => p.Category).AsNoTracking();

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p =>
                p.Title.Contains(q) ||
                p.Brand!.Name.Contains(q) ||
                p.Category!.Name.Contains(q)
            );
        }
        if (min.HasValue) query = query.Where(p => p.Price >= min.Value);
        if (max.HasValue) query = query.Where(p => p.Price <= max.Value);

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Title), // YENİ ...
            "name_desc" => query.OrderByDescending(p => p.Title), // YENİ ... performans ölçümü yapılmadı
            "newest" => query.OrderByDescending(p => p.Id),
            _ => query.OrderBy(p => p.Id)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminProductListDTO(
                p.Id,
                p.Title,
                p.Slug,
                p.Price,
                p.ImageUrl,
                p.BrandId,
                p.Brand!.Name,
                p.CategoryId,
                p.Category!.Name,
                p.Stock,
                p.IsActive
            ))
            .ToListAsync(ct);

        return new PagedResult<AdminProductListDTO>(total, page, pageSize, items);
    }

    public async Task<AdminProductDetailDTO?> GetDetailAdminAsync(int id, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new AdminProductDetailDTO(
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
                p.Description,
                p.Stock,
                p.IsActive
            ))
            .SingleOrDefaultAsync(ct);
    }



    public async Task<ProductDetailDTO?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id && p.IsActive)
            .Select(ProductDetailProjection())
            .SingleOrDefaultAsync(ct);
    }

    public async Task<ProductDetailDTO?> GetDetailBySlugAsync(string slug, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.Slug == slug && p.IsActive)
            .Select(ProductDetailProjection())
            .SingleOrDefaultAsync(ct);
    }

    // Ürün detay projeksiyonu — options + variants dahil; varyant fiyatı yoksa ürün fiyatı (etkin fiyat)
    private static System.Linq.Expressions.Expression<Func<Markadan.Domain.Models.Product, ProductDetailDTO>> ProductDetailProjection()
        => p => new ProductDetailDTO(
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
            p.Description,
            p.Options
                .OrderBy(o => o.SortOrder)
                .Select(o => new ProductOptionDTO(
                    o.Id,
                    o.Name,
                    o.Values
                        .OrderBy(v => v.SortOrder)
                        .Select(v => new ProductOptionValueDTO(v.Id, v.Value))
                        .ToList()))
                .ToList(),
            p.Variants
                .Where(v => v.IsActive)
                .Select(v => new ProductVariantDTO(
                    v.Id,
                    v.Sku,
                    v.Price ?? p.Price,
                    v.Stock,
                    v.ImageUrl,
                    v.SelectedValues.Select(sv => sv.OptionValueId).ToList()))
                .ToList());

    public async Task<PagedResult<ProductListDTO>> ListAsync(
        int? categoryId,
        int? brandId,
        string? q,
        decimal? min,
        decimal? max,
        string? sort,
        int page = 1,
        int pageSize = 12,
        CancellationToken ct = default)

    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 12;

        var query = _db.Products.AsNoTracking().Where(p => p.IsActive);

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Title.Contains(q));
        if (min.HasValue) query = query.Where(p => p.Price >= min.Value);
        if (max.HasValue) query = query.Where(p => p.Price <= max.Value);

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.Id),
            _ => query.OrderBy(p => p.Id)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListDTO(
                p.Id,
                p.Title,
                p.Slug,
                p.Price,
                p.ImageUrl,
                p.BrandId,
                p.Brand!.Name,
                p.CategoryId,
                p.Category!.Name
            ))
            .ToListAsync(ct);

        return new PagedResult<ProductListDTO>(total, page, pageSize, items);
    }

    public async Task<IReadOnlyList<AdminProductExportRow>> ExportAdminAsync(string? q, string? sort, CancellationToken ct = default)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Title.Contains(q) || p.Brand!.Name.Contains(q) || p.Category!.Name.Contains(q));

        query = sort switch
        {
            "price_asc"  => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc"   => query.OrderBy(p => p.Title),
            "name_desc"  => query.OrderByDescending(p => p.Title),
            "newest"     => query.OrderByDescending(p => p.Id),
            _            => query.OrderBy(p => p.Id)
        };

        return await query
            .Select(p => new AdminProductExportRow(
                p.Title,
                p.Description,
                p.Price,
                p.Stock,
                p.ImageUrl,
                p.Brand!.Name,
                p.Category!.Name))
            .ToListAsync(ct);
    }

    public async Task<List<ProductListDTO>> GetRelatedAsync(int productId, int limit = 6, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new { p.CategoryId, p.Price })
            .FirstOrDefaultAsync(ct);

        if (product is null) return [];

        // Aynı kategoriden, stokta olan (basit ürün stoğu veya aktif varyant stoğu), fiyata göre en yakın
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == product.CategoryId
                     && p.Id != productId
                     && p.IsActive
                     && (p.Stock > 0 || p.Variants.Any(v => v.IsActive && v.Stock > 0)))
            .OrderBy(p => Math.Abs((double)(p.Price - product.Price)))
            .Take(limit)
            .Select(p => new ProductListDTO(
                p.Id, p.Title, p.Slug, p.Price, p.ImageUrl,
                p.BrandId, p.Brand!.Name, p.CategoryId, p.Category!.Name))
            .ToListAsync(ct);
    }
}
