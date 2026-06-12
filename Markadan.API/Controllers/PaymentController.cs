using Markadan.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers;

// iyzico'nun server-to-server callback ucu — production'da iyzico bu adrese POST atar
[ApiController]
[Route("payment")]
public sealed class PaymentController : ControllerBase
{
    private readonly ICheckoutService _checkout;

    public PaymentController(ICheckoutService checkout) => _checkout = checkout;

    [HttpPost("iyzico/callback")]
    public async Task<IActionResult> IyzicoCallback([FromForm] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest();

        try
        {
            await _checkout.ConfirmPaymentAsync(token, HttpContext.RequestAborted);
            return Ok();
        }
        catch
        {
            // iyzico callback başarısız olsa da 200 dönülür — iyzico retry yapar
            return Ok();
        }
    }
}
