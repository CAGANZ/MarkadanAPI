using Markadan.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Markadan.API.Controllers;

[ApiController]
[Route("debug")]
public class DebugController : ControllerBase
{
    private readonly MarkadanDbContext _db;
    public DebugController(MarkadanDbContext db) => _db = db;

    [HttpGet("db")]
    public async Task<IActionResult> Db()
    {
        var conn = _db.Database.GetDbConnection();
        var dataSource = conn.DataSource; 
        var database = conn.Database;

        var brands = await _db.Brands.CountAsync();
        var categories = await _db.Categories.CountAsync();
        var products = await _db.Products.CountAsync();

        return Ok(new { dataSource, database, brands, categories, products });
    }
}
