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

        var query = _db.Products.AsNoTracking();

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
            .Select(p => new AdminProductListDTO(
                p.Id,
                p.Title,
                p.Price,
                p.ImageUrl,
                p.BrandId,
                p.Brand!.Name,
                p.CategoryId,
                p.Category!.Name,
                p.Stock
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
                p.Price,
                p.ImageUrl,
                p.BrandId,
                p.Brand!.Name,
                p.CategoryId,
                p.Category!.Name,
                p.Description,
                p.Stock
            ))
            .SingleOrDefaultAsync(ct);
    }



    public async Task<ProductDetailDTO?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
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
            .SingleOrDefaultAsync(ct);
    }

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

        var query = _db.Products.AsNoTracking();

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
}
