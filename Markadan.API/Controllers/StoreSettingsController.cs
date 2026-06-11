using Markadan.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

// Public — frontend marka/tema bilgisini sayfa yüklenirken buradan okur
[ApiController]
[Route("store-settings")]
public sealed class StoreSettingsController : ControllerBase
{
    private readonly IStoreSettingsService _svc;

    public StoreSettingsController(IStoreSettingsService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _svc.GetAsync(HttpContext.RequestAborted));
}
