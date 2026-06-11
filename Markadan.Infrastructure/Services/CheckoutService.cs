using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Carts;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Markadan.Infrastructure.Services;

public sealed class CheckoutService : ICheckoutService
{
    private readonly MarkadanDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<CheckoutService> _log;
    private readonly int _lowStockThreshold;
    private readonly string? _adminEmail;

    public CheckoutService(MarkadanDbContext db, IEmailService email, ILogger<CheckoutService> log, IConfiguration config)
    {
        _db                = db;
        _email             = email;
        _log               = log;
        _lowStockThreshold = config.GetValue<int>("Inventory:LowStockThreshold", 5);
        _adminEmail        = config["Inventory:AdminEmail"];
    }

    public async Task<CartDTO> CheckoutAsync(int userId, int addressId, CancellationToken ct = default)
    {
        // 1. Active sepeti ürünleriyle yükle (tracking — sonra güncelleyeceğiz)
        var cart = await _db.Carts
            .AsTracking()
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Where(c => c.AppUserId == userId && c.Status == CartStatus.Active)
            .FirstOrDefaultAsync(ct)
            ?? throw new BusinessRuleException("Aktif sepet bulunamadı.");

        // 2. Boş sepet kontrolü
        if (!cart.Items.Any())
            throw new BusinessRuleException("Sepet boş.");

        // 3. Fiyat snapshot kontrolü — fiyat değiştiyse müşteri sepeti yenilemeli
        var changedItems = cart.Items
            .Where(i => i.UnitPriceSnapshot != i.Product.Price)
            .Select(i => i.Product.Title)
            .ToList();

        if (changedItems.Count > 0)
            throw new BusinessRuleException(
                $"Bazı ürünlerin fiyatı değişti: {string.Join(", ", changedItems)}. " +
                "Güncel fiyatları görmek için sepeti yenileyiniz.");

        // 4. Adres kullanıcıya ait mi?
        var address = await _db.Addresses
            .Where(a => a.Id == addressId && a.AppUserId == userId)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Adres bulunamadı.");

        // 5. Transaction — stok düşme + sepet güncelleme atomik olmalı
        // EnableRetryOnFailure ile manuel transaction, ExecutionStrategy içinde sarmalanmalı
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            foreach (var item in cart.Items)
            {
                // Koşullu UPDATE: stock < quantity ise 0 satır etkilenir → yetersiz stok
                var affected = await _db.Products
                    .Where(p => p.Id == item.ProductId && p.Stock >= item.Quantity)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity), ct);

                if (affected == 0)
                    throw new BusinessRuleException(
                        $"'{item.Product.Title}' ürünü için yeterli stok yok.");
            }

            // 6. Sepeti siparişe çevir
            cart.Status        = CartStatus.Ordered;
            cart.OrderedAtUtc  = DateTime.UtcNow;
            cart.UpdatedAt     = DateTime.UtcNow;
            cart.OrderNumber   = GenerateOrderNumber();

            // Adres FK + snapshot (kullanıcı adresi sonradan silse de sipariş geçmişi korunur)
            cart.ShippingAddressId  = address.Id;
            cart.ShippingStreet     = address.Street;
            cart.ShippingCity       = address.City;
            cart.ShippingState      = address.State;
            cart.ShippingPostalCode = address.PostalCode;
            cart.ShippingCountry    = address.Country;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        // 7. Stok uyarısı — fire-and-forget, checkout yanıtını engellemesin
        _ = NotifyLowStockAsync(cart.Items.Select(i => i.ProductId).ToList(), CancellationToken.None);

        // 8. Yanıt — in-memory yüklü veriden build edilir (ekstra sorgu yok)
        var items = cart.Items.Select(i => new CartItemDTO(
            Id:                i.Id,
            ProductId:         i.ProductId,
            Title:             i.Product.Title,
            ImageUrl:          i.Product.ImageUrl,
            UnitPriceSnapshot: i.UnitPriceSnapshot,
            CurrentPrice:      i.Product.Price,
            PriceChanged:      false,               // fiyat kontrolünden geçti, değişmedi
            Quantity:          i.Quantity,
            Subtotal:          i.UnitPriceSnapshot * i.Quantity
        )).ToList();

        return new CartDTO(
            Id:              cart.Id,
            Status:          cart.Status.ToString(),
            Items:           items,
            Total:           items.Sum(i => i.Subtotal),
            HasPriceChanges: false
        );
    }

    private async Task NotifyLowStockAsync(List<int> productIds, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_adminEmail)) return;

        try
        {
            var lowStock = await _db.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id) && p.Stock <= _lowStockThreshold)
                .Select(p => new { p.Title, p.Stock })
                .ToListAsync(ct);

            if (lowStock.Count == 0) return;

            var rows = string.Join("", lowStock.Select(p =>
                $"<tr><td>{p.Title}</td><td><strong>{p.Stock} adet</strong></td></tr>"));

            var body = $"""
                <p>Aşağıdaki ürünlerin stoğu kritik seviyenin altına ({_lowStockThreshold} adet) düştü:</p>
                <table border="1" cellpadding="6" style="border-collapse:collapse">
                  <thead><tr><th>Ürün</th><th>Kalan Stok</th></tr></thead>
                  <tbody>{rows}</tbody>
                </table>
                <p>Stok takibini güncelleyebilirsiniz.</p>
                """;

            await _email.SendAsync(_adminEmail, "Mağaza Yöneticisi",
                $"Stok uyarısı: {lowStock.Count} üründe kritik seviye", body, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Stok uyarısı maili gönderilemedi");
        }
    }

    // MRK-XXXXXXXX formatında tahmin edilemez sipariş numarası
    private static string GenerateOrderNumber()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var sb = new StringBuilder("MRK-", 12);
        foreach (var b in bytes)
            sb.Append(chars[b % chars.Length]);
        return sb.ToString();
    }
}
