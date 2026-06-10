using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Orders;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class OrderCommandService : IOrderCommandService
{
    private readonly MarkadanDbContext _db;
    private readonly IOrderReadService _orderRead;

    public OrderCommandService(MarkadanDbContext db, IOrderReadService orderRead)
    {
        _db = db;
        _orderRead = orderRead;
    }

    public async Task<OrderDTO> CancelAsync(int userId, int orderId, CancellationToken ct = default)
    {
        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
            .Where(c => c.Id == orderId && c.AppUserId == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı.");

        if (cart.Status != CartStatus.Ordered)
            throw new BusinessRuleException("Yalnızca 'Ordered' durumundaki siparişler iptal edilebilir.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // Stok iadesi — her ürüne miktar geri eklenir
            foreach (var item in cart.Items)
            {
                await _db.Products
                    .Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity), ct);
            }

            cart.Status    = CartStatus.Cancelled;
            cart.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        return await _orderRead.GetOrderAsync(userId, orderId, ct);
    }
}
