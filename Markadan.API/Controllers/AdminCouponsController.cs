using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("admin/coupons")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminCouponsController : ControllerBase
{
    private readonly ICouponService _coupon;

    public AdminCouponsController(ICouponService coupon) => _coupon = coupon;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var coupons = await _coupon.GetAllAsync(HttpContext.RequestAborted);
        return Ok(coupons);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var coupon = await _coupon.GetByIdAsync(id, HttpContext.RequestAborted);
        return Ok(coupon);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCouponDTO input)
    {
        var coupon = await _coupon.CreateAsync(input, HttpContext.RequestAborted);
        return CreatedAtAction(nameof(GetById), new { id = coupon.Id }, coupon);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCouponDTO input)
    {
        var coupon = await _coupon.UpdateAsync(id, input, HttpContext.RequestAborted);
        return Ok(coupon);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _coupon.DeleteAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }
}
