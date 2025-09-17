using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Brands;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers
{
    [ApiController]
    [Route("admin/brands")]
    public class AdminBrandsController : ControllerBase
    {
        private readonly IBrandCommandService _commands;

        public AdminBrandsController(IBrandCommandService commands)
        {
            ArgumentNullException.ThrowIfNull(commands);
            _commands = commands;
        }

        [HttpPost]        
        public async Task<IActionResult> Create([FromBody] BrandCreateDTO input)
        {
            var created = await _commands.CreateAsync(input, HttpContext.RequestAborted);

            return CreatedAtAction(
                actionName: nameof(BrandsController.GetById),
                controllerName: "Brands",
                routeValues: new { id = created.Id },
                value: created
            );
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] BrandUpdateDTO input)
        {
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
}
