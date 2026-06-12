using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Orders;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Markadan.Infrastructure.Services;

public sealed class OrderCommandService : IOrderCommandService
{
    private readonly MarkadanDbContext _db;
    private readonly IOrderReadService _orderRead;
    private readonly IPaymentService _payment;
    private readonly ILogger<OrderCommandService> _log;

    public OrderCommandService(
        MarkadanDbContext db,
        IOrderReadService orderRead,
        IPaymentService payment,
        ILogger<OrderCommandService> log)
    {
        _db        = db;
        _orderRead = orderRead;
        _payment   = payment;
        _log       = log;
    }

    public async Task<OrderDTO> CancelAsync(int userId, int orderId, CancellationToken ct = default)
    {
        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
            .Where(c => c.Id == orderId && c.AppUserId == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı.");

        switch (cart.Status)
        {
            case CartStatus.PaymentPending:
                // Ödeme tamamlanmamış — serbest iptal, stok iadesi yok
                break;

            case CartStatus.Ordered:
            case CartStatus.Preparing:
                // Ödeme alınmış — önce iyzico iadesi
                if (cart.IyzicoPaymentId != null)
                {
                    var total    = cart.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity);
                    var refunded = await _payment.CancelPaymentAsync(cart.IyzicoPaymentId, total, "127.0.0.1", ct);
                    if (!refunded)
                        _log.LogWarning("iyzico iade başarısız — sipariş {OrderId} manuel incelenecek", orderId);
                }
                // Stok iadesi
                await RefundStockAsync(cart, ct);
                break;

            case CartStatus.Shipped:
                throw new BusinessRuleException("Kargoya verilmiş siparişler iptal edilemez. Lütfen teslim aldıktan sonra mağazayla iletişime geçin.");

            case CartStatus.Delivered:
                throw new BusinessRuleException("Teslim edilmiş siparişler iptal edilemez.");

            default:
                throw new BusinessRuleException("Bu sipariş iptal edilemez.");
        }

        cart.Status    = CartStatus.Cancelled;
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await _orderRead.GetOrderAsync(userId, orderId, ct);
    }

    private async Task RefundStockAsync(Markadan.Domain.Models.Cart cart, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            foreach (var item in cart.Items)
            {
                await _db.Products
                    .Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity), ct);
            }
            await tx.CommitAsync(ct);
        });
    }
}
