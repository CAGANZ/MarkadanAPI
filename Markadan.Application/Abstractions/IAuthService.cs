using Markadan.Application.DTOs.Auth;
using Markadan.Application.DTOs.Users;
using System.Security.Claims;

namespace Markadan.Application.Abstractions;

public interface IAuthService
{
    Task<LoginResultDTO> RegisterAsync(RegisterRequestDTO dto, CancellationToken ct = default);
    Task<LoginResultDTO?> LoginAsync(LoginRequestDTO dto, CancellationToken ct = default);
    Task<MeDTO?> GetMeAsync(ClaimsPrincipal principal, CancellationToken ct = default);
    Task<LoginResultDTO?> RefreshAsync(RefreshTokenRequestDTO dto, string? ip, CancellationToken ct = default);



}
