using Markadan.Application.DTOs.Addresses;

namespace Markadan.Application.Abstractions;

public interface IAddressService
{
    Task<IReadOnlyList<AddressDTO>> GetAllAsync(int userId, CancellationToken ct = default);
    Task<AddressDTO?> GetByIdAsync(int id, int userId, CancellationToken ct = default);
    Task<AddressDTO> CreateAsync(AddressCreateDTO dto, int userId, CancellationToken ct = default);
    Task<AddressDTO?> UpdateAsync(AddressUpdateDTO dto, int userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default);
}
