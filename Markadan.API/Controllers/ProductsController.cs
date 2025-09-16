using Markadan.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("products")]
public class ProductsController : ControllerBase
{
    public readonly IProductReadService _products;
    public ProductsController(IProductReadService products)
    {
        ArgumentNullException.ThrowIfNull(products);
        _products = products;
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

        return Ok(result); // PagedResult<ProductListDTO>
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var dto = await _products.GetDetailAsync(id, HttpContext.RequestAborted);
        return dto is null ? NotFound() : Ok(dto);
    }
}
