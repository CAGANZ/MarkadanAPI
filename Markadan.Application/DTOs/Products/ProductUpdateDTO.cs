using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Products
{
    public record ProductUpdateDTO(
        int Id,
        [MinLength(1), MaxLength(200)] string? Title = null,
        string? Description = null,
        [Range(0.0, double.MaxValue)] decimal? Price = null,
        [Range(0, int.MaxValue)] int? Stock = null,
        string? ImageUrl = null,
        [Range(1, int.MaxValue)] int? BrandId = null,
        [Range(1, int.MaxValue)] int? CategoryId = null
    );
}
