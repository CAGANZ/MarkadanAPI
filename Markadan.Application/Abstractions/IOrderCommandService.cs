using Markadan.Application.DTOs.Orders;

namespace Markadan.Application.Abstractions;

public interface IOrderCommandService
{
    Task<OrderDTO> CancelAsync(int userId, int orderId, CancellationToken ct = default);
}
