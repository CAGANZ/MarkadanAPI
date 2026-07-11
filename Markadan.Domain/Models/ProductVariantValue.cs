namespace Markadan.Domain.Models
{
    // Join: bir varyant ↔ bir seçenek değeri (M:N)
    public class ProductVariantValue
    {
        public int VariantId { get; set; }
        public ProductVariant Variant { get; set; } = default!;

        public int OptionValueId { get; set; }
        public ProductOptionValue OptionValue { get; set; } = default!;
    }
}
