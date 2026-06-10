using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Brands;

public record BrandUpdateDTO
{
    public int Id { get; init; }
    [MinLength(1), MaxLength(150)]
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
}
