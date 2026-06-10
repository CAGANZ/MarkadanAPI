using Markadan.Application.DTOs.Orders;

namespace Markadan.Application.Abstractions;

public interface IAdminOrderReadService
{
    Task<IReadOnlyList<AdminOrderSummaryDTO>> GetOrdersAsync(
        string? status, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);

    Task<AdminOrderDTO> GetOrderAsync(int orderId, CancellationToken ct = default);
}
