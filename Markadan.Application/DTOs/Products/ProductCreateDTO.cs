using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Products
{
    public record ProductCreateDTO(
        [Required, MinLength(1), MaxLength(200)] string Title,
        string? Description,
        [Range(0.0, double.MaxValue)] decimal Price,
        [Range(0, int.MaxValue)] int Stock,
        string? ImageUrl,
        [Range(1, int.MaxValue)] int BrandId,
        [Range(1, int.MaxValue)] int CategoryId
    );
}
