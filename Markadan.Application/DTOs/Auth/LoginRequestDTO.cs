namespace Markadan.Application.DTOs.Auth
{
    public record LoginRequestDTO( 
        string UserNameOrEmail,  
        string Password
        );
}
