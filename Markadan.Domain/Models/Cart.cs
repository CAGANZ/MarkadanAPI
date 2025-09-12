using Markadan.Domain.Models.Enums;

namespace Markadan.Domain.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }
        public CartStatus Status { get; set; } = CartStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
