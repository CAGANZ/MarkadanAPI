namespace Markadan.Domain.Models;

public class WishlistItem
{
    public int Id { get; set; }
    public int AppUserId { get; set; }
    public required AppUser AppUser { get; set; }
    public int ProductId { get; set; }
    public required Product Product { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
