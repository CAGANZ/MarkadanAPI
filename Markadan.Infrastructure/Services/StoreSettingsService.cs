using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Settings;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class StoreSettingsService : IStoreSettingsService
{
    private readonly MarkadanDbContext _db;

    public StoreSettingsService(MarkadanDbContext db) => _db = db;

    public async Task<StoreSettingsDTO> GetAsync(CancellationToken ct = default)
    {
        var s = await GetOrCreateAsync(ct);
        return ToDTO(s);
    }

    public async Task<StoreSettingsDTO> UpdateAsync(UpdateStoreSettingsDTO dto, CancellationToken ct = default)
    {
        var s = await GetOrCreateAsync(ct);

        s.StoreName       = dto.StoreName.Trim();
        s.LogoUrl         = Nullify(dto.LogoUrl);
        s.Description     = Nullify(dto.Description);
        s.WhatsAppPhone   = Nullify(dto.WhatsAppPhone);
        s.ContactPhone    = Nullify(dto.ContactPhone);
        s.ContactEmail    = Nullify(dto.ContactEmail);
        s.InstagramUrl    = Nullify(dto.InstagramUrl);
        s.FacebookUrl     = Nullify(dto.FacebookUrl);
        s.PrimaryColor    = Nullify(dto.PrimaryColor);
        s.AccentColor     = Nullify(dto.AccentColor);
        s.MetaDescription = Nullify(dto.MetaDescription);
        s.UpdatedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDTO(s);
    }

    // İlk çağrıda satır yoksa varsayılan oluşturur — migration sonrası boş DB'de de çalışır
    private async Task<StoreSettings> GetOrCreateAsync(CancellationToken ct)
    {
        var s = await _db.StoreSettings.FirstOrDefaultAsync(ct);
        if (s is not null) return s;

        s = new StoreSettings { StoreName = "Mağaza" };
        _db.StoreSettings.Add(s);
        await _db.SaveChangesAsync(ct);
        return s;
    }

    private static StoreSettingsDTO ToDTO(StoreSettings s) => new(
        s.StoreName, s.LogoUrl, s.Description,
        s.WhatsAppPhone, s.ContactPhone, s.ContactEmail,
        s.InstagramUrl, s.FacebookUrl,
        s.PrimaryColor, s.AccentColor, s.MetaDescription
    );

    private static string? Nullify(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
