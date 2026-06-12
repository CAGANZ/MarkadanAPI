using Markadan.Application.DTOs.Carts;
using Markadan.Application.DTOs.Orders;
using Markadan.Application.DTOs.Payment;

namespace Markadan.Application.Abstractions;

public interface ICheckoutService
{
    Task<CartDTO> CheckoutAsync(int userId, int addressId, CancellationToken ct = default);
    Task<InitiateCheckoutResponseDTO> InitiatePaymentAsync(int userId, int addressId, string userIp, CancellationToken ct = default);
    Task<OrderDTO> ConfirmPaymentAsync(string token, CancellationToken ct = default);
}
