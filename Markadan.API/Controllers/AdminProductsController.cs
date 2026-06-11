using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("admin/products")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IProductReadService _reads;
    private readonly IProductCommandService _commands;

    public AdminProductsController(IProductCommandService commands, IProductReadService reads)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(reads);
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

    // POST /admin/products/bulk — multipart/form-data, alan adı "file", UTF-8 CSV
    // Başlık satırı: Title,Description,Price,Stock,ImageUrl,BrandName,CategoryName
    [HttpPost("bulk")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> BulkUpload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("CSV dosyası gönderilmedi.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Yalnızca .csv uzantılı dosya kabul edilir.");

        await using var stream = file.OpenReadStream();
        var result = await _commands.BulkUploadAsync(stream, HttpContext.RequestAborted);
        return Ok(result);
    }
}