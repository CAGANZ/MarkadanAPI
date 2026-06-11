using System.Security.Claims;
using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("me/wishlist")]
[Authorize]
public sealed class MeWishlistController : ControllerBase
{
    private readonly IWishlistService _wishlist;

    public MeWishlistController(IWishlistService wishlist) => _wishlist = wishlist;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var items = await _wishlist.GetAsync(userId, HttpContext.RequestAborted);
        return Ok(items);
    }

    [HttpPost("items")]
    public async Task<IActionResult> Add([FromBody] AddWishlistItemDTO input)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var item = await _wishlist.AddAsync(userId, input.ProductId, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(Get), item);
    }

    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> Remove([FromRoute] int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var removed = await _wishlist.RemoveAsync(userId, id, HttpContext.RequestAborted);
        return removed ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out userId);
    }
}
