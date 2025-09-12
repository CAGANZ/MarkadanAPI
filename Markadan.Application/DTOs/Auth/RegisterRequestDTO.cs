namespace Markadan.Application.DTOs.Auth
{
    public record RegisterRequestDTO(
        string Email,
        string UserName,
        string Password,
        string Name,
        string Surname
        );
}
