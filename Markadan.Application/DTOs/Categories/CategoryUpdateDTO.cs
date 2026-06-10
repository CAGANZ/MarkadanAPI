using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Categories
{
    public record CategoryUpdateDTO
    {
        public int Id { get; init; }
        [MinLength(1), MaxLength(150)]
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}
