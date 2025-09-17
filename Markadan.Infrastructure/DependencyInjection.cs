using Markadan.Application.Abstractions;
using Markadan.Infrastructure.Data;
using Markadan.Infrastructure.Services;
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
                 ?? throw new InvalidOperationException("Connection string bulunamadı (DefaultConnection veya Default).");

        services.AddDbContext<MarkadanDbContext>(o =>
            o.UseSqlServer(cs, sql =>
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));


        services.AddScoped<IProductReadService, ProductReadService>();
        services.AddScoped<IProductCommandService, ProductCommandService>();

        services.AddScoped<IBrandReadService, BrandReadService>();
        services.AddScoped<IBrandCommandService, BrandCommandService>();

        services.AddScoped<ICategoryReadService, CategoryReadService>();


        return services;
    }
}