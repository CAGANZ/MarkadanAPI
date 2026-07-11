using System.Text;
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
            .Where(c => c.Status != CartStatus.Active && c.Status != CartStatus.PaymentPending);

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
                c.TrackingNumber,
                c.TrackingUrl,
                UserEmail = c.AppUser.Email!,
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

        return new AdminOrderDTO(
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
            order.AppUserId,
            order.UserEmail,
            order.TrackingNumber,
            order.TrackingUrl
        );
    }

    public async Task<byte[]> ExportCsvAsync(
        string? status, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var query = _db.Carts
            .AsNoTracking()
            .Where(c => c.Status != CartStatus.Active && c.Status != CartStatus.PaymentPending);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CartStatus>(status, ignoreCase: true, out var parsed))
            query = query.Where(c => c.Status == parsed);

        if (dateFrom.HasValue)
            query = query.Where(c => c.OrderedAtUtc >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(c => c.OrderedAtUtc <= dateTo.Value);

        var rows = await query
            .OrderByDescending(c => c.OrderedAtUtc)
            .Select(c => new
            {
                c.OrderNumber,
                c.OrderedAtUtc,
                c.Status,
                UserName    = c.AppUser.Name + " " + c.AppUser.Surname,
                UserEmail   = c.AppUser.Email!,
                ItemCount   = c.Items.Count,
                GrossTotal  = c.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity),
                c.DiscountAmount,
                c.CouponCode,
                c.TrackingNumber,
                c.TrackingUrl,
                c.ShippingCity,
                c.ShippingCountry,
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();

        // BOM: Excel Türkçe karakterleri doğru okusun diye
        sb.Append('﻿');

        // Başlık satırı
        sb.AppendLine("Sipariş No,Tarih,Durum,Müşteri Adı,E-posta,Ürün Sayısı,Brüt Tutar,İndirim,Net Tutar,Kupon Kodu,Kargo Takip No,Kargo URL,Şehir,Ülke");

        foreach (var r in rows)
        {
            var netTotal = r.GrossTotal - r.DiscountAmount;
            sb.AppendLine(string.Join(",",
                Csv(r.OrderNumber),
                Csv(r.OrderedAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? ""),
                Csv(r.Status.ToString()),
                Csv(r.UserName),
                Csv(r.UserEmail),
                r.ItemCount.ToString(),
                r.GrossTotal.ToString("F2"),
                r.DiscountAmount.ToString("F2"),
                netTotal.ToString("F2"),
                Csv(r.CouponCode ?? ""),
                Csv(r.TrackingNumber ?? ""),
                Csv(r.TrackingUrl ?? ""),
                Csv(r.ShippingCity ?? ""),
                Csv(r.ShippingCountry ?? "")
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // RFC 4180: değer virgül veya tırnak içeriyorsa çift tırnak ile sarar, tırnakları escape eder
    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
