using Markadan.Domain.Models.Enums;

namespace Markadan.Domain.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public required AppUser AppUser { get; set; } // buraya tekrar bakacağım şimdilik required kalsın arştıracağım...
        public CartStatus Status { get; set; } = CartStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
