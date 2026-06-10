using System.Security.Claims;
using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("me/checkout")]
[Authorize]
public sealed class MeCheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkout;

    public MeCheckoutController(ICheckoutService checkout) => _checkout = checkout;

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDTO input)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var order = await _checkout.CheckoutAsync(userId, input.AddressId, HttpContext.RequestAborted);
        return Ok(order);
    }

    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out userId);
    }
}
