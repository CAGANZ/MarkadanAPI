using Markadan.Application.DTOs.Brands;
using Markadan.Application.Exceptions;
using Markadan.Infrastructure.Services;
using Markadan.Tests.Helpers;

namespace Markadan.Tests.BrandTests;

public sealed class BrandCommandServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
    private readonly Markadan.Infrastructure.Data.MarkadanDbContext _db;
    private readonly BrandCommandService _svc;

    public BrandCommandServiceTests()
    {
        (_db, _conn) = TestDbFactory.Create();
        _svc = new BrandCommandService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsBusinessRuleException()
    {
        await _svc.CreateAsync(new BrandCreateDTO { Name = "Nike" });

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _svc.CreateAsync(new BrandCreateDTO { Name = "Nike" }));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsBusinessRuleException()
    {
        await _svc.CreateAsync(new BrandCreateDTO { Name = "Adidas" });
        var second = await _svc.CreateAsync(new BrandCreateDTO { Name = "Puma" });

        // "Puma" adını "Adidas" ile değiştirmeye çalış
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _svc.UpdateAsync(new BrandUpdateDTO { Id = second.Id, Name = "Adidas" }));
    }
}
