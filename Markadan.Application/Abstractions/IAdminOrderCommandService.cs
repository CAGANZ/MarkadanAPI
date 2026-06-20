using Markadan.Application.DTOs.Orders;

namespace Markadan.Application.Abstractions;

public interface IAdminOrderCommandService
{
    Task<AdminOrderDTO> UpdateStatusAsync(
        int orderId, string status,
        string? trackingNumber = null, string? trackingUrl = null,
        CancellationToken ct = default);
}
