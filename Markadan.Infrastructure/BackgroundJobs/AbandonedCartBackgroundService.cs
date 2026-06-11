using Markadan.Application.Abstractions;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Markadan.Infrastructure.BackgroundJobs;

public sealed class AbandonedCartBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AbandonedCartBackgroundService> _log;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _abandonThreshold;

    public AbandonedCartBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AbandonedCartBackgroundService> log,
        IConfiguration config)
    {
        _scopeFactory      = scopeFactory;
        _log               = log;
        _checkInterval     = TimeSpan.FromMinutes(config.GetValue<int>("AbandonedCart:CheckIntervalMinutes", 30));
        _abandonThreshold  = TimeSpan.FromHours(config.GetValue<double>("AbandonedCart:ThresholdHours", 2));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk çalışmayı biraz geciktir — uygulama tam ayağa kalksın
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Terk edilen sepet job hatası");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task SendRemindersAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - _abandonThreshold;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db    = scope.ServiceProvider.GetRequiredService<MarkadanDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Active, en az 1 ürün içeren, eşikten eski, henüz mail gönderilmemiş sepetler
        var carts = await db.Carts
            .Where(c => c.Status == CartStatus.Active
                     && c.Items.Any()
                     && c.AbandonedReminderSentAt == null
                     && (c.UpdatedAt ?? c.CreatedAt) < cutoff)
            .Include(c => c.AppUser)
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync(ct);

        foreach (var cart in carts)
        {
            var user = cart.AppUser;
            if (string.IsNullOrEmpty(user.Email)) continue;

            try
            {
                var itemRows = string.Join("", cart.Items.Select(i =>
                    $"<tr><td>{i.Product?.Title}</td><td>{i.Quantity} adet</td><td>{i.UnitPriceSnapshot:N2} ₺</td></tr>"));

                var body = $"""
                    <p>Merhaba {user.Name},</p>
                    <p>Sepetinizde ürünler bekleniyor! Alışverişinizi tamamlamak için tıklayın.</p>
                    <table border="1" cellpadding="6" style="border-collapse:collapse">
                      <thead><tr><th>Ürün</th><th>Miktar</th><th>Fiyat</th></tr></thead>
                      <tbody>{itemRows}</tbody>
                    </table>
                    <p>Sepetiniz sizi bekliyor!</p>
                    """;

                await email.SendAsync(user.Email, $"{user.Name} {user.Surname}", "Sepetinizde ürünler var!", body, ct);

                cart.AbandonedReminderSentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Terk edilen sepet maili gönderilemedi: userId={UserId}", user.Id);
            }
        }

        if (carts.Count > 0)
            await db.SaveChangesAsync(ct);

        _log.LogInformation("Terk edilen sepet job: {Count} mail gönderildi", carts.Count);
    }
}
