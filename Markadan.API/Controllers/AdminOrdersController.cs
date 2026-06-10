using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("admin/orders")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderReadService _orderRead;
    private readonly IAdminOrderCommandService _orderCommand;

    public AdminOrdersController(IAdminOrderReadService orderRead, IAdminOrderCommandService orderCommand)
    {
        _orderRead = orderRead;
        _orderCommand = orderCommand;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        var orders = await _orderRead.GetOrdersAsync(status, dateFrom, dateTo, HttpContext.RequestAborted);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _orderRead.GetOrderAsync(id, HttpContext.RequestAborted);
        return Ok(order);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDTO input)
    {
        var order = await _orderCommand.UpdateStatusAsync(id, input.Status, HttpContext.RequestAborted);
        return Ok(order);
    }
}
