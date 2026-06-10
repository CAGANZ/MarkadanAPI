using Markadan.Domain.Models.Enums;

namespace Markadan.Domain.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public required AppUser AppUser { get; set; }
        public CartStatus Status { get; set; } = CartStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Sipariş alanları — Active sepette null; checkout sonrası dolar
        public string? OrderNumber { get; set; }
        public DateTime? OrderedAtUtc { get; set; }

        // Adres FK (SetNull): kullanıcı adresi silerse FK null olur ama snapshot alanlar korunur
        public int? ShippingAddressId { get; set; }
        public Address? ShippingAddress { get; set; }

        // Checkout anında adres alanlarının kopyası — sipariş geçmişi immutable kalır
        public string? ShippingStreet { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingState { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCountry { get; set; }

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
