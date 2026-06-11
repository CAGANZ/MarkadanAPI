namespace Markadan.Application.DTOs.Settings;

// Public endpoint'te döner — frontend marka bilgisini buradan okur
public record StoreSettingsDTO(
    string StoreName,
    string? LogoUrl,
    string? Description,
    string? WhatsAppPhone,
    string? ContactPhone,
    string? ContactEmail,
    string? InstagramUrl,
    string? FacebookUrl,
    string? PrimaryColor,
    string? AccentColor,
    string? MetaDescription
);
