namespace Markadan.Domain.Models;

public sealed class RefreshToken
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }

    public AppUser? AppUser { get; set; }
}
