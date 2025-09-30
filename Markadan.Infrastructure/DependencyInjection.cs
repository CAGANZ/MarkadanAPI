using Markadan.Application.Abstractions;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Markadan.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Markadan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("DefaultConnection")
                 ?? config.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("Connection string bulunamadı!? (DefaultConnection veya Default).");

        services.AddDbContext<MarkadanDbContext>(o =>
            o.UseSqlServer(cs, sql =>
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        services
        .AddIdentityCore<AppUser>(opt =>
        {
            opt.User.RequireUniqueEmail = true;
            opt.Password.RequiredLength = 6;
            opt.Password.RequireNonAlphanumeric = false;
            opt.Password.RequireUppercase = false;
            opt.Password.RequireLowercase = false;
            opt.Password.RequireDigit = false;
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<MarkadanDbContext>()
        .AddSignInManager();


        services.AddScoped<IProductReadService, ProductReadService>();
        services.AddScoped<IProductCommandService, ProductCommandService>();

        services.AddScoped<IBrandReadService, BrandReadService>();
        services.AddScoped<IBrandCommandService, BrandCommandService>();

        services.AddScoped<ICategoryReadService, CategoryReadService>();
        services.AddScoped<ICategoryCommandService, CategoryCommandService>();

        services.AddScoped<IAuthService, AuthService>();


        return services;
    }
}