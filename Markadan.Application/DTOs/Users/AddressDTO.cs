namespace Markadan.Application.DTOs.Users
{
    public record AddressDTO(
        int Id,
        string AddressName,
        string Street,
        string City,
        string State,
        string PostalCode,
        string Country
    );
}
