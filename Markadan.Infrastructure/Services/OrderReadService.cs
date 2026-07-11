using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Orders;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class OrderReadService : IOrderReadService
{
    private readonly MarkadanDbContext _db;

    public OrderReadService(MarkadanDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderSummaryDTO>> GetOrdersAsync(int userId, CancellationToken ct = default)
    {
        return await _db.Carts
            .AsNoTracking()
            .Where(c => c.AppUserId == userId && c.Status != CartStatus.Active && c.Status != CartStatus.PaymentPending)
            .OrderByDescending(c => c.OrderedAtUtc)
            .Select(c => new OrderSummaryDTO(
                c.Id,
                c.OrderNumber!,
                c.Status.ToString(),
                c.OrderedAtUtc!.Value,
                c.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity),
                c.Items.Count
            ))
            .ToListAsync(ct);
    }

    public async Task<OrderDTO> GetOrderAsync(int userId, int orderId, CancellationToken ct = default)
    {
        var order = await _db.Carts
            .AsNoTracking()
            .Where(c => c.Id == orderId && c.AppUserId == userId && c.Status != CartStatus.Active)
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
                c.TrackingNumber,
                c.TrackingUrl,
                Items = c.Items.Select(i => new
                {
                    i.ProductId,
                    Title           = i.Product.Title,
                    ProductImageUrl = i.Product.ImageUrl,
                    VariantImageUrl = i.ProductVariant != null ? i.ProductVariant.ImageUrl : null,
                    i.UnitPriceSnapshot,
                    i.Quantity,
                    i.ProductVariantId,
                    VariantValues   = i.ProductVariant != null
                        ? i.ProductVariant.SelectedValues
                            .OrderBy(sv => sv.OptionValue.Option.SortOrder)
                            .Select(sv => sv.OptionValue.Value)
                            .ToList()
                        : null
                }).ToList()
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı.");

        var items = order.Items.Select(i => new OrderItemDTO(
            i.ProductId,
            i.Title,
            i.VariantImageUrl ?? i.ProductImageUrl,
            i.UnitPriceSnapshot,
            i.Quantity,
            i.UnitPriceSnapshot * i.Quantity,
            i.ProductVariantId,
            i.VariantValues != null && i.VariantValues.Count > 0 ? string.Join(" / ", i.VariantValues) : null
        )).ToList();

        return new OrderDTO(
            order.Id,
            order.OrderNumber!,
            order.Status.ToString(),
            order.OrderedAtUtc!.Value,
            items.Sum(i => i.Subtotal),
            items,
            order.ShippingStreet,
            order.ShippingCity,
            order.ShippingState,
            order.ShippingPostalCode,
            order.ShippingCountry,
            order.TrackingNumber,
            order.TrackingUrl
        );
    }
}
