using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Orders;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Markadan.Infrastructure.Services;

public sealed class AdminOrderCommandService : IAdminOrderCommandService
{
    private readonly MarkadanDbContext _db;
    private readonly IAdminOrderReadService _orderRead;
    private readonly IPaymentService _payment;
    private readonly ILogger<AdminOrderCommandService> _log;

    // İzin verilen admin durum geçişleri
    private static readonly Dictionary<CartStatus, HashSet<CartStatus>> AllowedTransitions = new()
    {
        [CartStatus.Ordered]    = [CartStatus.Preparing, CartStatus.Cancelled],
        [CartStatus.Preparing]  = [CartStatus.Shipped, CartStatus.Cancelled],
        [CartStatus.Shipped]    = [CartStatus.Delivered, CartStatus.Cancelled],
        [CartStatus.Delivered]  = [CartStatus.Cancelled],
    };

    public AdminOrderCommandService(
        MarkadanDbContext db,
        IAdminOrderReadService orderRead,
        IPaymentService payment,
        ILogger<AdminOrderCommandService> log)
    {
        _db        = db;
        _orderRead = orderRead;
        _payment   = payment;
        _log       = log;
    }

    public async Task<AdminOrderDTO> UpdateStatusAsync(int orderId, string status, CancellationToken ct = default)
    {
        if (!Enum.TryParse<CartStatus>(status, ignoreCase: true, out var newStatus))
            throw new BusinessRuleException($"Geçersiz sipariş durumu: '{status}'.");

        if (newStatus == CartStatus.Active || newStatus == CartStatus.PaymentPending)
            throw new BusinessRuleException($"Sipariş durumu '{newStatus}' olarak güncellenemez.");

        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
            .Where(c => c.Id == orderId && c.Status != CartStatus.Active)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Sipariş bulunamadı.");

        if (cart.Status == newStatus)
            throw new BusinessRuleException($"Sipariş zaten '{newStatus}' durumunda.");

        if (!AllowedTransitions.TryGetValue(cart.Status, out var allowed) || !allowed.Contains(newStatus))
            throw new BusinessRuleException(
                $"'{cart.Status}' → '{newStatus}' geçişi yapılamaz. " +
                $"İzin verilen: {string.Join(", ", allowed ?? [])}.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            if (newStatus == CartStatus.Cancelled)
            {
                // Ödeme iade: Ordered veya Preparing → iyzico iptali
                if (cart.IyzicoPaymentId != null &&
                    (cart.Status == CartStatus.Ordered || cart.Status == CartStatus.Preparing))
                {
                    var total    = cart.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity);
                    var refunded = await _payment.CancelPaymentAsync(cart.IyzicoPaymentId, total, "127.0.0.1", ct);
                    if (!refunded)
                        _log.LogWarning("Admin iptal: iyzico iade başarısız — sipariş {OrderId} manuel incelenecek", orderId);
                }
                else if (cart.Status == CartStatus.Shipped || cart.Status == CartStatus.Delivered)
                {
                    _log.LogWarning("Admin iptal: kargo sonrası iptal — sipariş {OrderId}, manuel iade gerekli", orderId);
                }

                // Stok iadesi (Shipped/Delivered hariç — fiziksel ürün geri gelmedi)
                if (cart.Status == CartStatus.Ordered || cart.Status == CartStatus.Preparing)
                {
                    foreach (var item in cart.Items)
                    {
                        await _db.Products
                            .Where(p => p.Id == item.ProductId)
                            .ExecuteUpdateAsync(
                                s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity), ct);
                    }
                }
            }

            cart.Status    = newStatus;
            cart.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        return await _orderRead.GetOrderAsync(orderId, ct);
    }
}
