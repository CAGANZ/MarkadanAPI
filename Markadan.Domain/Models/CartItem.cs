namespace Markadan.Domain.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }  // varyantlı ürünlerde dolu, basit ürünlerde null
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public Cart Cart { get; set; } = default!;
        public Product Product { get; set; } = default!;
        public ProductVariant? ProductVariant { get; set; }
    }
}
