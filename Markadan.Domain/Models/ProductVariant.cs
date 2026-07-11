namespace Markadan.Domain.Models
{
    // Seçeneklerin bir kombinasyonu: "Kırmızı + M". Gerçek stok ve fiyat burada tutulur.
    public class ProductVariant
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public string? Sku { get; set; }
        public decimal? Price { get; set; }  // null → ürünün fiyatı; dolu → bu varyantın özel fiyatı
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        // Bu varyantı oluşturan seçenek değerleri (Kırmızı, M)
        public ICollection<ProductVariantValue> SelectedValues { get; set; } = new List<ProductVariantValue>();
    }
}
