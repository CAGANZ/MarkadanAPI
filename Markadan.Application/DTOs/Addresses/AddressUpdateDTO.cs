using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Addresses;

public record AddressUpdateDTO
{
    public int Id { get; init; }

    [MaxLength(100)]
    public string? AddressName { get; init; }

    [MaxLength(200)]
    public string? Street { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? State { get; init; }

    [MaxLength(20)]
    public string? PostalCode { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }
}
