using Markadan.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Markadan.API.Controllers;

[ApiController]
[Route("brands")]
public class BrandsController : ControllerBase
{
    private readonly MarkadanDbContext _db;
    public BrandsController(MarkadanDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var items = await _db.Brands.AsNoTracking()
            .Select(b => new {
                b.Id,
                b.Name
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        if (!int.TryParse(id, out var brandId))
        {
            return BadRequest("Invalid brand ID format.");
        }

        var brand = await _db.Brands.AsNoTracking()
            .Where(b => b.Id == brandId)
            .Select(b => new {
                b.Id,
                b.Name
            })
            .FirstOrDefaultAsync();

        return brand is null ? NotFound() : Ok(brand);
    }
}
