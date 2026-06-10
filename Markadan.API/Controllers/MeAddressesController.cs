using System.Security.Claims;
using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("me/addresses")]
[Authorize]
public sealed class MeAddressesController : ControllerBase
{
    private readonly IAddressService _addresses;

    public MeAddressesController(IAddressService addresses) => _addresses = addresses;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var list = await _addresses.GetAllAsync(userId, HttpContext.RequestAborted);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var address = await _addresses.GetByIdAsync(id, userId, HttpContext.RequestAborted);
        return address is null ? NotFound() : Ok(address);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddressCreateDTO input)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var created = await _addresses.CreateAsync(input, userId, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AddressUpdateDTO input)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var dto = input with { Id = id };
        var updated = await _addresses.UpdateAsync(dto, userId, HttpContext.RequestAborted);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var ok = await _addresses.DeleteAsync(id, userId, HttpContext.RequestAborted);
        return ok ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out userId);
    }
}
