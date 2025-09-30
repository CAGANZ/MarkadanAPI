using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Auth;
using Markadan.Application.DTOs.Users;
using Markadan.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Markadan.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO input, CancellationToken ct = default)
        {
            var result = await _auth.RegisterAsync(input, ct);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResultDTO>> Login([FromBody] LoginRequestDTO dto, CancellationToken ct)
        {
            var result = await _auth.LoginAsync(dto, ct);
            return result is null ? Unauthorized() : Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<MeDTO>> Me(CancellationToken ct)
        {
            var me = await _auth.GetMeAsync(User, ct);
            return me is null ? Unauthorized() : Ok(me);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResultDTO>> Refresh([FromBody] RefreshTokenRequestDTO dto, CancellationToken ct)
        {
            // IP’yi istemiyorsan null bırakabilirsin; istersen HttpContext.Connection.RemoteIpAddress?.ToString()
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _auth.RefreshAsync(dto, ip, ct);
            return result is null ? Unauthorized() : Ok(result);
        }


    }
}
