using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

[ApiController]
[Route("admin/settings")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminSettingsController : ControllerBase
{
    private readonly IStoreSettingsService _svc;

    public AdminSettingsController(IStoreSettingsService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _svc.GetAsync(HttpContext.RequestAborted));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateStoreSettingsDTO dto)
        => Ok(await _svc.UpdateAsync(dto, HttpContext.RequestAborted));
}
