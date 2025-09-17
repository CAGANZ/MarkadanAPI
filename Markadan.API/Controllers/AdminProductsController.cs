using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Products;
using Markadan.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("admin/products")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly MarkadanDbContext _db;
    private readonly IProductReadService _reads;
    private readonly IProductCommandService _commands;

    public AdminProductsController(MarkadanDbContext db, IProductCommandService commands, IProductReadService reads)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(reads);
        _db = db;
        _commands = commands;
        _reads = reads;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductCreateDTO input)
    {
        var created = await _commands.CreateAsync(input, HttpContext.RequestAborted);

        return CreatedAtAction(
            actionName: nameof(ProductsController.GetById),
            controllerName: "Products",
            routeValues: new { id = created.Id },
            value: created
        );
    }

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
        var result = await _reads.ListAdminAsync(
            categoryId,
            brandId,
            q,
            min,
            max,
            sort,
            page,
            pageSize,
            HttpContext.RequestAborted);
        return Ok(result);
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var dto = await _reads.GetDetailAdminAsync(id, HttpContext.RequestAborted);
        return dto is null ? NotFound() : Ok(dto);
    }


    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ProductUpdateDTO input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var dto = input with { Id = id };

        var updated = await _commands.UpdateAsync(dto, HttpContext.RequestAborted);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var ok = await _commands.DeleteAsync(id, HttpContext.RequestAborted);
        return ok ? NoContent() : NotFound();
    }
}