using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Settings;

public record UpdateStoreSettingsDTO(
    [Required][MaxLength(100)] string StoreName,
    [MaxLength(500)] string? LogoUrl,
    [MaxLength(500)] string? Description,
    [MaxLength(20)]  string? WhatsAppPhone,
    [MaxLength(20)]  string? ContactPhone,
    [MaxLength(200)] string? ContactEmail,
    [MaxLength(200)] string? InstagramUrl,
    [MaxLength(200)] string? FacebookUrl,
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Renk #RRGGBB formatında olmalı")]
    [MaxLength(7)]   string? PrimaryColor,
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Renk #RRGGBB formatında olmalı")]
    [MaxLength(7)]   string? AccentColor,
    [MaxLength(300)] string? MetaDescription
);
