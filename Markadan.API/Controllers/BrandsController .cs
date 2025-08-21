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
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync();

        return Ok(items);
    }
}
