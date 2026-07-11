using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Products.Variants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("admin/products/{productId:int}")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminProductVariantsController : ControllerBase
{
    private readonly IProductVariantService _service;

    public AdminProductVariantsController(IProductVariantService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _service = service;
    }

    // ── Opsiyonlar (eksenler) ─────────────────────────────────────────────────

    [HttpGet("options")]
    public async Task<IActionResult> GetOptions([FromRoute] int productId)
        => Ok(await _service.GetOptionsAsync(productId, HttpContext.RequestAborted));

    [HttpPost("options")]
    public async Task<IActionResult> AddOption([FromRoute] int productId, [FromBody] CreateOptionDTO input)
        => Ok(await _service.AddOptionAsync(productId, input, HttpContext.RequestAborted));

    [HttpDelete("options/{optionId:int}")]
    public async Task<IActionResult> DeleteOption([FromRoute] int productId, [FromRoute] int optionId)
        => await _service.DeleteOptionAsync(productId, optionId, HttpContext.RequestAborted)
            ? NoContent() : NotFound();

    [HttpPost("options/{optionId:int}/values")]
    public async Task<IActionResult> AddOptionValue([FromRoute] int productId, [FromRoute] int optionId, [FromBody] CreateOptionValueDTO input)
        => Ok(await _service.AddOptionValueAsync(productId, optionId, input, HttpContext.RequestAborted));

    [HttpDelete("option-values/{valueId:int}")]
    public async Task<IActionResult> DeleteOptionValue([FromRoute] int productId, [FromRoute] int valueId)
        => await _service.DeleteOptionValueAsync(productId, valueId, HttpContext.RequestAborted)
            ? NoContent() : NotFound();

    // ── Varyantlar ─────────────────────────────────────────────────────────────

    [HttpGet("variants")]
    public async Task<IActionResult> GetVariants([FromRoute] int productId)
        => Ok(await _service.GetVariantsAsync(productId, HttpContext.RequestAborted));

    [HttpPost("variants")]
    public async Task<IActionResult> AddVariant([FromRoute] int productId, [FromBody] CreateVariantDTO input)
        => Ok(await _service.AddVariantAsync(productId, input, HttpContext.RequestAborted));

    [HttpPut("variants/{variantId:int}")]
    public async Task<IActionResult> UpdateVariant([FromRoute] int productId, [FromRoute] int variantId, [FromBody] UpdateVariantDTO input)
    {
        var dto = await _service.UpdateVariantAsync(productId, variantId, input, HttpContext.RequestAborted);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpDelete("variants/{variantId:int}")]
    public async Task<IActionResult> DeleteVariant([FromRoute] int productId, [FromRoute] int variantId)
        => await _service.DeleteVariantAsync(productId, variantId, HttpContext.RequestAborted)
            ? NoContent() : NotFound();
}
