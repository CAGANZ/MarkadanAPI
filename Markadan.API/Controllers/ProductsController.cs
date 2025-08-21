using Markadan.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Markadan.API.Controllers;

[ApiController]
[Route("products")]
public class ProductsController : ControllerBase
{
    private readonly MarkadanDbContext _db;
    public ProductsController(MarkadanDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int? categoryId,
        [FromQuery] int? brandId,
        [FromQuery] string? q,
        [FromQuery] decimal? min,
        [FromQuery] decimal? max,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0 || pageSize > 100) pageSize = 12;

        var query = _db.Products.AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsQueryable();

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Title.Contains(term));
        }
        if (min.HasValue) query = query.Where(p => p.Price >= min.Value);
        if (max.HasValue) query = query.Where(p => p.Price <= max.Value);

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "price_desc" => query.OrderByDescending(p => p.Price).ThenByDescending(p => p.Id),
            "newest" => query.OrderByDescending(p => p.Id), // CreatedAt olmadığı için Id ~ newest
            _ => query.OrderBy(p => p.Id)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new {
                p.Id,
                p.Title,
                p.Price,
                p.Stock,
                p.ImageUrl,
                p.BrandId,
                BrandName = p.Brand!.Name,
                p.CategoryId,
                CategoryName = p.Category!.Name
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.Products.AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .Where(x => x.Id == id)
            .Select(x => new {
                x.Id,//14
                x.Title,
                x.Price,
                x.Stock,
                x.ImageUrl,
                x.BrandId,
                BrandName = x.Brand!.Name,
                x.CategoryId,
                CategoryName = x.Category!.Name
            })
            .FirstOrDefaultAsync();

        return p is null ? NotFound() : Ok(p);
    }
}
