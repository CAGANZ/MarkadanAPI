using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Orders;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class AdminOrderCommandService : IAdminOrderCommandService
{
    private readonly MarkadanDbContext _db;
    private readonly IAdminOrderReadService _orderRead;

    public AdminOrderCommandService(MarkadanDbContext db, IAdminOrderReadService orderRead)
    {
        _db = db;
        _orderRead = orderRead;
    }

    public async Task<AdminOrderDTO> UpdateStatusAsync(int orderId, string status, CancellationToken ct = default)
    {
        if (!Enum.TryParse<CartStatus>(status, ignoreCase: true, out var newStatus))
            throw new BusinessRuleException($"Geçersiz sipariş durumu: '{status}'.");

        if (newStatus == CartStatus.Active)
            throw new BusinessRuleException("Sipariş durumu 'Active' olarak güncellenemez.");

        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
            .Where(c => c.Id == orderId && c.Status != CartStatus.Active)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı.");

        if (cart.Status == newStatus)
            throw new BusinessRuleException($"Sipariş zaten '{newStatus}' durumunda.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Cancelled'a geçişte stok iade edilir
        if (newStatus == CartStatus.Cancelled && cart.Status == CartStatus.Ordered)
        {
            foreach (var item in cart.Items)
            {
                await _db.Products
                    .Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity), ct);
            }
        }

        cart.Status    = newStatus;
        cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await _orderRead.GetOrderAsync(orderId, ct);
    }
}
