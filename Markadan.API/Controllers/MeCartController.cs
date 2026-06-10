using System.Security.Claims;
using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("me/cart")]
[Authorize]
public sealed class MeCartController : ControllerBase
{
    private readonly ICartService _cart;

    public MeCartController(ICartService cart) => _cart = cart;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var dto = await _cart.GetActiveCartAsync(userId, HttpContext.RequestAborted);
        return Ok(dto);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemDTO input)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var dto = await _cart.AddItemAsync(userId, input, HttpContext.RequestAborted);
        return Ok(dto);
    }

    [HttpPut("items/{id:int}")]
    public async Task<IActionResult> UpdateItem([FromRoute] int id, [FromBody] UpdateCartItemQuantityDTO input)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var dto = await _cart.UpdateItemQuantityAsync(userId, id, input.Quantity, HttpContext.RequestAborted);
        return Ok(dto);
    }

    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> RemoveItem([FromRoute] int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var dto = await _cart.RemoveItemAsync(userId, id, HttpContext.RequestAborted);
        return Ok(dto);
    }

    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var dto = await _cart.ClearAsync(userId, HttpContext.RequestAborted);
        return Ok(dto);
    }

    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out userId);
    }
}
