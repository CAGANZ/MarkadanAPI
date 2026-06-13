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

    // iyzico popup akışı — adım 1: ödeme başlat, token al
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] CheckoutRequestDTO input)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var rawIp = HttpContext.Connection.RemoteIpAddress;
        var ip = rawIp?.IsIPv4MappedToIPv6 == true
            ? rawIp.MapToIPv4().ToString()
            : rawIp?.ToString() ?? "127.0.0.1";
        var result = await _checkout.InitiatePaymentAsync(userId, input.AddressId, ip, HttpContext.RequestAborted);
        return Ok(result);
    }

    // iyzico popup akışı — adım 2: popup kapandıktan sonra frontend ödemeyi doğrulat
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmPaymentDTO input)
    {
        if (!TryGetUserId(out _)) return Unauthorized();
        var order = await _checkout.ConfirmPaymentAsync(input.Token, HttpContext.RequestAborted);
        return Ok(order);
    }

    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out userId);
    }
}

public record ConfirmPaymentDTO(string Token);
