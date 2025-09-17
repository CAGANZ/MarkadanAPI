using Markadan.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("brands")]
public sealed class BrandsController : ControllerBase
{
    private readonly IBrandReadService _brands;

    public BrandsController(IBrandReadService brands)
    {
        ArgumentNullException.ThrowIfNull(brands);
        _brands = brands;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var list = await _brands.GetAllAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var dto = await _brands.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

}
