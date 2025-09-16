
using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Products;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("admin/products")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IProductCommandService _commands;
    public AdminProductsController(IProductCommandService commands) => _commands = commands;

    // POST /admin/products
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductCreateDTO input)
    {
        var created = await _commands.CreateAsync(input, HttpContext.RequestAborted);

        // 201 + Location: /products/{id}  (public detay endpoint’ine işaret ediyor)
        return CreatedAtAction(
            actionName: nameof(ProductsController.GetById),
            controllerName: "Products",
            routeValues: new { id = created.Id },
            value: created
        );
    }

    //[HttpPut("{id:int}")]
    //public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ProductUpdateDTO input)
    //{
    //    if (id != input.Id)
    //        return BadRequest(new ProblemDetails { Title = "Route id and body id must match." });

    //    var updated = await _commands.UpdateAsync(input, HttpContext.RequestAborted);
    //    return updated is null ? NotFound() : Ok(updated);
    //}

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ProductUpdateDTO input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Route id tek gerçek: body'deki id'yi ez
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
