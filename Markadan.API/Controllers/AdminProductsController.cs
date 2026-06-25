using System.Text;
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

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? q,
        [FromQuery] string? sort)
    {
        var rows = await _reads.ExportAdminAsync(q, sort, HttpContext.RequestAborted);
        var bytes = BuildProductCsv(rows);
        var filename = $"urunler_{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }

    [HttpPatch("{id:int}/active")]
    public async Task<IActionResult> SetActive([FromRoute] int id, [FromBody] SetActiveDTO input)
    {
        var ok = await _commands.SetActiveAsync(id, input.IsActive, HttpContext.RequestAborted);
        return ok ? Ok() : NotFound();
    }

    // POST /admin/products/bulk — multipart/form-data, alan adı "file", UTF-8 CSV
    // Başlık satırı: Title,Description,Price,Stock,ImageUrl,BrandName,CategoryName
    [HttpPost("bulk")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("bulk-upload")]
    public async Task<IActionResult> BulkUpload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("CSV dosyası gönderilmedi.");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Yalnızca .csv uzantılı dosya kabul edilir.");

        // Binary içerik kontrolü — gerçek metin olmayan dosyaları reddet
        await using var stream = file.OpenReadStream();
        var peek = new byte[4];
        int read = await stream.ReadAsync(peek, HttpContext.RequestAborted);
        if (read >= 2 && peek[0] == 0xFF && peek[1] == 0xFE) { } // UTF-16 LE BOM — kabul
        else if (read >= 3 && peek[0] == 0xEF && peek[1] == 0xBB && peek[2] == 0xBF) { } // UTF-8 BOM — kabul
        else
        {
            for (int i = 0; i < read; i++)
            {
                if (peek[i] < 0x09 || (peek[i] > 0x0D && peek[i] < 0x20 && peek[i] != 0x1B))
                    return BadRequest("Dosya içeriği metin/CSV formatında değil.");
            }
        }
        stream.Seek(0, System.IO.SeekOrigin.Begin);

        var result = await _commands.BulkUploadAsync(stream, HttpContext.RequestAborted);
        return Ok(result);
    }

    private static byte[] BuildProductCsv(IReadOnlyList<AdminProductExportRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Title,Description,Price,Stock,ImageUrl,BrandName,CategoryName");
        foreach (var r in rows)
        {
            sb.Append(CsvCell(r.Title)).Append(',');
            sb.Append(CsvCell(r.Description)).Append(',');
            sb.Append(r.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.Stock).Append(',');
            sb.Append(CsvCell(r.ImageUrl)).Append(',');
            sb.Append(CsvCell(r.BrandName)).Append(',');
            sb.AppendLine(CsvCell(r.CategoryName));
        }
        // BOM + UTF-8
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string CsvCell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        // Formül enjeksiyonu koruması
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = "'" + value;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}