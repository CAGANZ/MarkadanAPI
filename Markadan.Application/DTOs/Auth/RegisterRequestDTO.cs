using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Auth;

public record RegisterRequestDTO
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string UserName { get; init; }

    [Required]
    public required string Password { get; init; }

    [Required, Phone]
    public required string PhoneNumber { get; init; }

    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Surname { get; init; }

    [Required]
    public required string GovId { get; init; }

    [Required]
    public required DateTime Birthday { get; init; }
}
