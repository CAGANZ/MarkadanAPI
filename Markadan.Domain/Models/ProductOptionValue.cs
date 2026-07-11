namespace Markadan.Domain.Models
{
    // Bir eksendeki değer: "Kırmızı", "M", "256GB"...
    public class ProductOptionValue
    {
        public int Id { get; set; }
        public int OptionId { get; set; }
        public ProductOption Option { get; set; } = default!;

        public string Value { get; set; } = "";
        public int SortOrder { get; set; }

        public ICollection<ProductVariantValue> VariantValues { get; set; } = new List<ProductVariantValue>();
    }
}
