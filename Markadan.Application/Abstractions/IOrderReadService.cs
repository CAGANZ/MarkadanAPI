using Markadan.Application.DTOs.Orders;

namespace Markadan.Application.Abstractions;

public interface IOrderReadService
{
    Task<IReadOnlyList<OrderSummaryDTO>> GetOrdersAsync(int userId, CancellationToken ct = default);
    Task<OrderDTO> GetOrderAsync(int userId, int orderId, CancellationToken ct = default);
}
