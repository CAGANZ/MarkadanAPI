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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        return Ok(items);
    }
}
