using Markadan.Application.Abstractions;
using Markadan.Application.Options;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Markadan.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Markadan.Tests.Helpers;

/// <summary>
/// AuthService testleri için tam Identity + DbContext ortamı kurar.
/// Her test instance'ı izole bir SQLite in-memory DB kullanır.
/// </summary>
public sealed class IdentityServiceFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public IdentityServiceFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _connection.CreateCollation("Turkish_100_CI_AI",
            (x, y) => string.Compare(x, y, StringComparison.OrdinalIgnoreCase));

        var services = new ServiceCollection();

        services.AddDbContext<MarkadanDbContext>(opt => opt.UseSqlite(_connection));

        services.AddIdentityCore<AppUser>(opt =>
        {
            opt.Password.RequiredLength = 6;
            opt.Password.RequireNonAlphanumeric = false;
            opt.Password.RequireUppercase = false;
            opt.Password.RequireLowercase = false;
            opt.Password.RequireDigit = false;
            opt.User.RequireUniqueEmail = true;
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<MarkadanDbContext>()
        .AddSignInManager();

        services.AddDataProtection();

        services.AddSingleton(Options.Create(new JwtOptions
        {
            Key                = "test-super-secret-key-minimum-32bytes!",
            Issuer             = "test-issuer",
            Audience           = "test-audience",
            AccessTokenMinutes = 60
        }));

        services.AddScoped<IAuthService, AuthService>();

        _provider = services.BuildServiceProvider();

        // Şemayı bir kez oluştur
        using var init = _provider.CreateScope();
        init.ServiceProvider.GetRequiredService<MarkadanDbContext>().Database.EnsureCreated();
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
