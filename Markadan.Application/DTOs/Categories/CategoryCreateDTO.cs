namespace Markadan.Application.DTOs.Categories
{
    public record CategoryCreateDTO
    {
        public required string Name { get; init; }
        public string? Description { get; init; }
        public string? ImageUrl { get; init; }
    }
}
