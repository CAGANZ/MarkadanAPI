using Markadan.Application.DTOs.Carts;
using Markadan.Application.DTOs.Orders;

namespace Markadan.Application.Abstractions;

public interface IOrderCommandService
{
    Task<OrderDTO> CancelAsync(int userId, int orderId, CancellationToken ct = default);
    Task<CartDTO> ReorderAsync(int userId, int orderId, CancellationToken ct = default);
}
