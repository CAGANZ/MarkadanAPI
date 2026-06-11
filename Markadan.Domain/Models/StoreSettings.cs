namespace Markadan.Domain.Models;

// Tek satır — her instance'ın kendine ait mağaza kimliği
public class StoreSettings
{
    public int Id { get; set; }

    // Kimlik
    public required string StoreName { get; set; }
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }

    // İletişim
    public string? WhatsAppPhone { get; set; }   // ülke koduyla, örn. 905321234567
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }

    // Sosyal medya
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }

    // Tema (hex renk, örn. #4F46E5)
    public string? PrimaryColor { get; set; }
    public string? AccentColor { get; set; }

    // SEO
    public string? MetaDescription { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
