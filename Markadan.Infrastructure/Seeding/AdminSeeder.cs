using Markadan.Domain.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Markadan.Infrastructure.Seeding;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var adminEmail = config["Seed:AdminEmail"];
        var adminPassword = config["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            Console.WriteLine("UYARI: Seed:AdminEmail veya Seed:AdminPassword eksik — admin seed atlanıyor.");
            return;
        }

        var users = services.GetRequiredService<UserManager<AppUser>>();
        var roles = services.GetRequiredService<RoleManager<AppRole>>();
        var govIdProtector = services.GetRequiredService<IDataProtectionProvider>()
                                     .CreateProtector("Markadan.GovId.v1");

        const string adminRole = "Admin";

        // 1) Admin rolü yoksa oluştur
        if (!await roles.RoleExistsAsync(adminRole))
            await roles.CreateAsync(new AppRole { Name = adminRole });

        // 2) Kullanıcı zaten varsa sadece rolü güvence altına al (idempotent)
        var existing = await users.FindByEmailAsync(adminEmail);
        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(existing, adminRole))
                await users.AddToRoleAsync(existing, adminRole);
            return;
        }

        // 3) İlk kez: admin kullanıcısı oluştur
        var admin = new AppUser
        {
            UserName    = adminEmail.Split('@')[0],
            Email       = adminEmail,
            EmailConfirmed = true,
            Name        = "Admin",
            Surname     = "User",
            GovId       = govIdProtector.Protect("SYSTEM_ADMIN"),
            Birthday    = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted   = false
        };

        var result = await users.CreateAsync(admin, adminPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Admin seed başarısız: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        await users.AddToRoleAsync(admin, adminRole);
    }
}
