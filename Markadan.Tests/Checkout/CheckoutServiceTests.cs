using Markadan.Application.Exceptions;
using Markadan.Domain.Models;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Services;
using Markadan.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Tests.Checkout;

public sealed class CheckoutServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
    private readonly Markadan.Infrastructure.Data.MarkadanDbContext _db;
    private readonly CheckoutService _svc;

    public CheckoutServiceTests()
    {
        (_db, _conn) = TestDbFactory.Create();
        _svc = new CheckoutService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    [Fact]
    public async Task Checkout_PriceChanged_ThrowsBusinessRuleException()
    {
        // Snapshot 100, ürün fiyatı 150'ye çıkarıldı
        var seed = await TestDataSeeder.SeedAsync(_db, productPrice: 100m);

        seed.Product.Price = 150m;
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _svc.CheckoutAsync(seed.User.Id, seed.Address.Id));
    }

    [Fact]
    public async Task Checkout_InsufficientStock_ThrowsBusinessRuleException()
    {
        // Stok 1, sepetteki miktar 2
        var seed = await TestDataSeeder.SeedAsync(_db, productStock: 1, cartItemQty: 2);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _svc.CheckoutAsync(seed.User.Id, seed.Address.Id));
    }

    [Fact]
    public async Task Checkout_AlreadyOrdered_ThrowsBusinessRuleException()
    {
        var seed = await TestDataSeeder.SeedAsync(_db);

        // İlk checkout → sepet Ordered'a geçer, Active sepet kalmaz
        await _svc.CheckoutAsync(seed.User.Id, seed.Address.Id);

        // İkinci deneme: Active sepet bulunamadı
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _svc.CheckoutAsync(seed.User.Id, seed.Address.Id));
    }

    [Fact]
    public async Task Checkout_StockRace_SecondUserFails_WhenStockIsOne()
    {
        // Ürün stok=1. Kullanıcı A ve B aynı ürünü almak istiyor.
        var seed1 = await TestDataSeeder.SeedAsync(_db, productStock: 1, userSuffix: "A");
        var seed2 = await TestDataSeeder.SeedAsync(_db, productStock: 0, userSuffix: "B");

        // Kullanıcı B'nin sepetini kullanıcı A'nın ürününe çevir (paylaşılan stok senaryosu)
        var cart2 = await _db.Carts
            .Include(c => c.Items)
            .FirstAsync(c => c.AppUserId == seed2.User.Id && c.Status == CartStatus.Active);

        _db.CartItems.RemoveRange(cart2.Items);
        _db.CartItems.Add(new CartItem
        {
            CartId            = cart2.Id,
            ProductId         = seed1.Product.Id,
            Quantity          = 1,
            UnitPriceSnapshot = seed1.Product.Price
        });
        await _db.SaveChangesAsync();

        // Kullanıcı A checkout → başarılı, DB'de stok 1→0 (ExecuteUpdateAsync ile)
        var result = await _svc.CheckoutAsync(seed1.User.Id, seed1.Address.Id);
        Assert.Equal("Ordered", result.Status);

        // Kullanıcı B checkout → koşullu UPDATE WHERE Stock >= 1 → 0 satır etkilenir → hata
        // (ExecuteUpdateAsync her zaman DB'yi sorgular, change tracker'ı atlar)
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _svc.CheckoutAsync(seed2.User.Id, seed2.Address.Id));
    }
}
