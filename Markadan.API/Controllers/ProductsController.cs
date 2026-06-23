using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Markadan.API.Controllers;

[ApiController]
[Route("products")]
[EnableRateLimiting("public-catalog")]
public class ProductsController : ControllerBase
{
    private readonly IProductReadService _products;
    private readonly IProductCommandService _productCmd;

    public ProductsController(IProductReadService products, IProductCommandService productCmd)
    {
        ArgumentNullException.ThrowIfNull(products);
        _products   = products;
        _productCmd = productCmd;
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
        var result = await _products.ListAsync(
            categoryId, brandId,
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
        var dto = await _products.GetDetailAsync(id, HttpContext.RequestAborted);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug)
    {
        var dto = await _products.GetDetailBySlugAsync(slug, HttpContext.RequestAborted);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:int}/related")]
    public async Task<IActionResult> GetRelated([FromRoute] int id, [FromQuery] int limit = 6)
    {
        if (limit is < 1 or > 20) limit = 6;
        var items = await _products.GetRelatedAsync(id, limit, HttpContext.RequestAborted);
        return Ok(items);
    }

    [HttpPost("{id:int}/notify-stock")]
    public async Task<IActionResult> SubscribeStockNotification([FromRoute] int id, [FromBody] StockNotifyRequestDTO input)
    {
        try
        {
            await _productCmd.SubscribeStockNotificationAsync(id, input.Email, input.Name, HttpContext.RequestAborted);
            return Ok(new { message = "Stok geldiğinde e-posta ile bildirileceksiniz." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
