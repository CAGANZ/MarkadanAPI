using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Auth;
using Markadan.Application.DTOs.Users;
using Markadan.Application.Options;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Markadan.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _users;
    private readonly RoleManager<AppRole> _roles;
    private readonly JwtOptions _jwt;
    private readonly MarkadanDbContext _db;

    public AuthService(UserManager<AppUser> users, RoleManager<AppRole> roles, IOptions<JwtOptions> jwt, MarkadanDbContext db)
    {
        _users = users;
        _roles = roles;
        _jwt = jwt.Value;
        _db = db;

    }

    public async Task<LoginResultDTO> RegisterAsync(RegisterRequestDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new InvalidOperationException("Email required.");
        if (string.IsNullOrWhiteSpace(dto.UserName)) throw new InvalidOperationException("UserName required.");
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber)) throw new InvalidOperationException("PhoneNumber required.");
        if (string.IsNullOrWhiteSpace(dto.Password)) throw new InvalidOperationException("Password required.");
        if (dto.Password.Length < 6) throw new InvalidOperationException("Password must be at least 6 characters.");
        if (string.IsNullOrWhiteSpace(dto.GovId)) throw new InvalidOperationException("GovId required.");
        if (dto.Birthday == default) throw new InvalidOperationException("Birthday required.");

        // db den kontrol yapılanları asenkron yazacaksın..
        if (await _users.FindByEmailAsync(dto.Email) is not null)
            throw new InvalidOperationException("Email already in use.");
        if (await _users.FindByNameAsync(dto.UserName) is not null)
            throw new InvalidOperationException("UserName already in use.");

        var user = new AppUser //
        {
            UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Name = dto.Name?.Trim() ?? "",
            Surname = dto.Surname?.Trim() ?? "",
            GovId = dto.GovId,
            Birthday = dto.Birthday,
            IsDeleted = false
        };

        var created = await _users.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
            throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        
        const string defaultRole = "User";
        if (!await _roles.RoleExistsAsync(defaultRole))
            await _roles.CreateAsync(new AppRole { Name = defaultRole });

        await _users.AddToRoleAsync(user, defaultRole);


        var roles = await _users.GetRolesAsync(user);
        var refresh = await IssueRefreshTokenAsync(user, null, ct);
        return GenerateLoginResult(user, roles, refresh);
    }

    private LoginResultDTO GenerateLoginResult(AppUser user, IList<string> roles, string refreshToken)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier,   user.Id.ToString()),
            new(ClaimTypes.Name,             user.UserName ?? ""),
            new(ClaimTypes.Email,            user.Email ?? ""),
            new(ClaimTypes.GivenName,        user.Name ?? ""),
            new(ClaimTypes.Surname,          user.Surname ?? ""),
            new(ClaimTypes.MobilePhone,     user.PhoneNumber ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwt.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        
        return new LoginResultDTO(
            AccessToken: accessToken,
            ExpiresAtUtc: expires,
            RefreshToken: refreshToken,
            UserId: user.Id,
            Name: user.Name ?? "",
            Surname: user.Surname ?? "",
            Email: user.Email ?? "",
            Roles: roles.ToArray(),
            IsAdmin: roles.Contains("Admin")
        );  
    }

    private static string NewRefreshTokenString(int numBytes = 64)
    {
        Span<byte> bytes = stackalloc byte[numBytes];
        RandomNumberGenerator.Fill(bytes);
        var s = Convert.ToBase64String(bytes);
        return s.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private async Task<string> IssueRefreshTokenAsync(AppUser user, string? createdByIp, CancellationToken ct)
    {
        var token = NewRefreshTokenString();
        var rt = new RefreshToken
        {
            AppUserId = user.Id,
            Token = token,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),// 30 gün geçerlilik verdim..
            CreatedByIp = createdByIp
        };
        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync(ct);
        return token;
    }



    public async Task<LoginResultDTO?> LoginAsync(LoginRequestDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.UserNameOrEmail) || string.IsNullOrWhiteSpace(dto.Password))
            return null;

        var byEmail = dto.UserNameOrEmail.Contains('@');
        var user = byEmail
            ? await _users.FindByEmailAsync(dto.UserNameOrEmail.Trim())
            : await _users.FindByNameAsync(dto.UserNameOrEmail.Trim());

        if (user is null || user.IsDeleted) return null;

        var ok = await _users.CheckPasswordAsync(user, dto.Password);
        if (!ok) return null;

        var roles = await _users.GetRolesAsync(user);
        var refresh = await IssueRefreshTokenAsync(user, null, ct);
        return GenerateLoginResult(user, roles, refresh);
    }

    public async Task<MeDTO?> GetMeAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        var idStr = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(idStr, out var userId)) return null;

        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted) return null;

        return new MeDTO(user.Id, user.Name ?? "", user.Surname ?? "", user.Email ?? "");
    }

    public async Task<LoginResultDTO?> RefreshAsync(RefreshTokenRequestDTO dto, string? ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken)) return null;

        // 1) DB'de token'ı bul
        var rt = await _db.RefreshTokens
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Token == dto.RefreshToken, ct);

        // 2) Token yok, süresi geçmiş veya zaten revoke edilmişse: 401
        if (rt is null) return null;
        if (rt.RevokedAtUtc is not null) return null;
        if (rt.ExpiresAtUtc <= DateTime.UtcNow) return null;

        // 3) Kullanıcıyı getir (soft-deleted ise 401)
        var user = await _users.FindByIdAsync(rt.AppUserId.ToString());
        if (user is null || user.IsDeleted) return null;

        // 4) Yeni refresh üret
        var newRefresh = await IssueRefreshTokenAsync(user, ip, ct);

        // 5) Eskiyi kapat (rotasyon)
        rt.RevokedAtUtc = DateTime.UtcNow;
        rt.ReplacedByToken = newRefresh;
        await _db.SaveChangesAsync(ct);

        // 6) Yeni access token
        var roles = await _users.GetRolesAsync(user);
        return GenerateLoginResult(user, roles, newRefresh);
    }
}
