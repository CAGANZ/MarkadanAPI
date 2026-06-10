using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Auth;
using Markadan.Infrastructure.Data;
using Markadan.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Markadan.Tests.AuthTests;

public sealed class AuthServiceTests : IDisposable
{
    private readonly IdentityServiceFactory _factory;

    public AuthServiceTests() => _factory = new IdentityServiceFactory();
    public void Dispose() => _factory.Dispose();

    private static RegisterRequestDTO TestUser(string suffix = "") => new()
    {
        Email       = $"user{suffix}@test.com",
        UserName    = $"testuser{suffix}",
        Password    = "Test123",
        PhoneNumber = "5555555555",
        Name        = "Test",
        Surname     = "User",
        GovId       = "12345678901",
        Birthday    = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task Refresh_RotatesToken_OldTokenRevoked()
    {
        using var scope = _factory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var login    = await auth.RegisterAsync(TestUser());
        var oldToken = login.RefreshToken;

        var refreshed = await auth.RefreshAsync(new RefreshTokenRequestDTO(oldToken), null);
        Assert.NotNull(refreshed);
        Assert.NotEqual(oldToken, refreshed.RefreshToken);

        // Eski token artık çalışmamalı (revoke edildi)
        var replayOld = await auth.RefreshAsync(new RefreshTokenRequestDTO(oldToken), null);
        Assert.Null(replayOld);
    }

    [Fact]
    public async Task Refresh_ReuseDetection_RevokesEntireChain()
    {
        using var scope = _factory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var db   = scope.ServiceProvider.GetRequiredService<MarkadanDbContext>();

        var login  = await auth.RegisterAsync(TestUser("2"));
        var token1 = login.RefreshToken;

        // Token1 → Token2 (rotasyon)
        var r2 = await auth.RefreshAsync(new RefreshTokenRequestDTO(token1), null);
        Assert.NotNull(r2);
        var token2 = r2.RefreshToken;

        // Revoke edilmiş token1 yeniden kullanıldı → zincir invalidasyonu
        var reuse = await auth.RefreshAsync(new RefreshTokenRequestDTO(token1), null);
        Assert.Null(reuse);

        // Token2 de artık geçersiz olmalı (zincir tamamen iptal)
        var useToken2 = await auth.RefreshAsync(new RefreshTokenRequestDTO(token2), null);
        Assert.Null(useToken2);

        // DB'de kullanıcıya ait aktif token kalmadığını doğrula
        var userId = login.UserId;
        var activeCount = await db.RefreshTokens
            .CountAsync(t => t.AppUserId == userId
                          && t.RevokedAtUtc == null
                          && t.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(0, activeCount);
    }
}
