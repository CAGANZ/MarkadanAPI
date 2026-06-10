using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Categories
{
    public record CategoryCreateDTO
    {
        [Required, MinLength(1), MaxLength(150)]
        public required string Name { get; init; }
        public string? Description { get; init; }
        public string? ImageUrl { get; init; }
    }
}
