namespace Markadan.Domain.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? MetaDescription { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }

        public int BrandId { get; set; }
        public Brand? Brand { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
