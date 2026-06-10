using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Orders;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class AdminOrderReadService : IAdminOrderReadService
{
    private readonly MarkadanDbContext _db;

    public AdminOrderReadService(MarkadanDbContext db) => _db = db;

    public async Task<IReadOnlyList<AdminOrderSummaryDTO>> GetOrdersAsync(
        string? status, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var query = _db.Carts
            .AsNoTracking()
            .Where(c => c.Status != CartStatus.Active);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CartStatus>(status, ignoreCase: true, out var parsed))
            query = query.Where(c => c.Status == parsed);

        if (dateFrom.HasValue)
            query = query.Where(c => c.OrderedAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(c => c.OrderedAtUtc <= dateTo.Value);

        return await query
            .OrderByDescending(c => c.OrderedAtUtc)
            .Select(c => new AdminOrderSummaryDTO(
                c.Id,
                c.OrderNumber!,
                c.Status.ToString(),
                c.OrderedAtUtc!.Value,
                c.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity),
                c.Items.Count,
                c.AppUserId,
                c.AppUser.Email!
            ))
            .ToListAsync(ct);
    }

    public async Task<AdminOrderDTO> GetOrderAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _db.Carts
            .AsNoTracking()
            .Where(c => c.Id == orderId && c.Status != CartStatus.Active)
            .Select(c => new
            {
                c.Id,
                c.OrderNumber,
                c.Status,
                c.OrderedAtUtc,
                c.ShippingStreet,
                c.ShippingCity,
                c.ShippingState,
                c.ShippingPostalCode,
                c.ShippingCountry,
                c.AppUserId,
                UserEmail = c.AppUser.Email!,
                Items = c.Items.Select(i => new OrderItemDTO(
                    i.ProductId,
                    i.Product.Title,
                    i.Product.ImageUrl,
                    i.UnitPriceSnapshot,
                    i.Quantity,
                    i.UnitPriceSnapshot * i.Quantity
                )).ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı.");

        return new AdminOrderDTO(
            order.Id,
            order.OrderNumber!,
            order.Status.ToString(),
            order.OrderedAtUtc!.Value,
            order.Items.Sum(i => i.Subtotal),
            order.Items,
            order.ShippingStreet,
            order.ShippingCity,
            order.ShippingState,
            order.ShippingPostalCode,
            order.ShippingCountry,
            order.AppUserId,
            order.UserEmail
        );
    }
}
