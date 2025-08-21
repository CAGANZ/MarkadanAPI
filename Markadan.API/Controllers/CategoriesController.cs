using Markadan.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Markadan.API.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController : ControllerBase
{
    private readonly MarkadanDbContext _db;
    public CategoriesController(MarkadanDbContext db) => _db = db;

    /// <summary>
    /// Tüm kategorileri listeler.
    /// </summary>
    /// <returns>Kategori listesi</returns>
    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var items = await _db.Categories.AsNoTracking()
            .Select(c => new {
                c.Id,
                c.Name
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        if (!int.TryParse(id, out var categoryId))
        {
            return BadRequest("Invalid category ID format.");
        }

        var category = await _db.Categories.AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => new {
                c.Id,
                c.Name
            })
            .FirstOrDefaultAsync();

        return category is null ? NotFound() : Ok(category);
    }
}
