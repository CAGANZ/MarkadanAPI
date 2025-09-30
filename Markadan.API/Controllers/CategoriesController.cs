using Markadan.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryReadService _categories;

    public CategoriesController(ICategoryReadService categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        _categories = categories;
    }


    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken ct = default)
    {
        var dto = await _categories.GetAllAsync(ct);
        return Ok(dto);

    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct = default)
    {
        var dto = await _categories.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
