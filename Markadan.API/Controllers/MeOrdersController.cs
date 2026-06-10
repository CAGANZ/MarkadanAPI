using System.Security.Claims;
using Markadan.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("me/orders")]
[Authorize]
public sealed class MeOrdersController : ControllerBase
{
    private readonly IOrderReadService _orderRead;
    private readonly IOrderCommandService _orderCommand;

    public MeOrdersController(IOrderReadService orderRead, IOrderCommandService orderCommand)
    {
        _orderRead = orderRead;
        _orderCommand = orderCommand;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var orders = await _orderRead.GetOrdersAsync(userId, HttpContext.RequestAborted);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var order = await _orderRead.GetOrderAsync(userId, id, HttpContext.RequestAborted);
        return Ok(order);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var order = await _orderCommand.CancelAsync(userId, id, HttpContext.RequestAborted);
        return Ok(order);
    }

    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out userId);
    }
}
