using Markadan.Application.DTOs.Carts;

namespace Markadan.Application.Abstractions;

public interface ICheckoutService
{
    Task<CartDTO> CheckoutAsync(int userId, int addressId, CancellationToken ct = default);
}
