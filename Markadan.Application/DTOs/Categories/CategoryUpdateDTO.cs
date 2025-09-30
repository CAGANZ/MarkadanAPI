namespace Markadan.Application.DTOs.Categories
{
    public record CategoryUpdateDTO
    {
        public int Id { get; init; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}
