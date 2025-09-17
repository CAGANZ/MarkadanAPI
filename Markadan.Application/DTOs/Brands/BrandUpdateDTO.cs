namespace Markadan.Application.DTOs.Brands;

public record BrandUpdateDTO
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
}
