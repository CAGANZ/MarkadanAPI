using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Categories;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers
{
    [ApiController]
    [Route("admin/categories")]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly ICategoryCommandService _commands;

        public AdminCategoriesController(ICategoryCommandService commands)
        {
            ArgumentNullException.ThrowIfNull(commands);
            _commands = commands;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDTO input, CancellationToken ct = default)
        {
            var created = await _commands.CreateAsync(input, ct);

            return CreatedAtAction(
                actionName: nameof(CategoriesController.GetById),
                controllerName: "Categories",
                routeValues: new { id = created.Id },
                value: created
            );
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryUpdateDTO input, CancellationToken ct)
        {
            var dto = input with { Id=id };
            var updated = await _commands.UpdateAsync(dto, ct);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _commands.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
