namespace Markadan.Application.DTOs.Brands;

public record BrandCreateDTO
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
}
