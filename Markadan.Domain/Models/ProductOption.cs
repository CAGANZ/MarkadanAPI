namespace Markadan.Domain.Models
{
    // Bir ürünün seçenek ekseni: "Renk", "Beden", "Kapasite"...
    public class ProductOption
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public string Name { get; set; } = "";
        public int SortOrder { get; set; }

        public ICollection<ProductOptionValue> Values { get; set; } = new List<ProductOptionValue>();
    }
}
