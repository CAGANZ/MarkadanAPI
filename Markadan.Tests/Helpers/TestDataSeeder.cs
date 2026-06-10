using Markadan.Domain.Models;
using Markadan.Domain.Models.Enums;
using Markadan.Infrastructure.Data;

namespace Markadan.Tests.Helpers;

public record CheckoutSeedData(AppUser User, Product Product, Address Address);

public static class TestDataSeeder
{
    /// <summary>
    /// Checkout testleri için tam test ortamı kurar.
    /// Her çağrıda benzersiz isimler kullanır — aynı DB'de paralel seed destekler.
    /// </summary>
    public static async Task<CheckoutSeedData> SeedAsync(
        MarkadanDbContext db,
        decimal productPrice  = 100m,
        int     productStock  = 5,
        int     cartItemQty   = 1,
        string  userSuffix    = "")
    {
        var brand    = new Brand    { Name = $"Brand{userSuffix}" };
        var category = new Category { Name = $"Category{userSuffix}" };
        db.Brands.Add(brand);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var user = new AppUser
        {
            UserName          = $"tester{userSuffix}",
            NormalizedUserName = $"TESTER{userSuffix}".ToUpperInvariant(),
            Email              = $"tester{userSuffix}@test.com",
            NormalizedEmail    = $"TESTER{userSuffix}@TEST.COM".ToUpperInvariant(),
            Name               = "Test",
            Surname            = "User",
            GovId              = "dummy-gov-id",
            Birthday           = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted          = false,
            SecurityStamp      = Guid.NewGuid().ToString()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var product = new Product
        {
            Title      = "Test Ürün",
            Price      = productPrice,
            Stock      = productStock,
            BrandId    = brand.Id,
            CategoryId = category.Id
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var address = new Address
        {
            AppUserId   = user.Id,
            AppUser     = user,
            AddressName = "Ev",
            Street      = "Test Sokak",
            City        = "İstanbul",
            State       = "İstanbul",
            PostalCode  = "34000",
            Country     = "Türkiye"
        };
        db.Addresses.Add(address);

        var cart = new Cart
        {
            AppUserId = user.Id,
            AppUser   = user,
            Status    = CartStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Carts.Add(cart);
        await db.SaveChangesAsync();

        var item = new CartItem
        {
            CartId            = cart.Id,
            ProductId         = product.Id,
            Quantity          = cartItemQty,
            UnitPriceSnapshot = productPrice
        };
        db.CartItems.Add(item);
        await db.SaveChangesAsync();

        return new CheckoutSeedData(user, product, address);
    }
}
