using Markadan.Application.Abstractions;
using Markadan.Application.DTOs.Wishlist;
using Markadan.Application.Exceptions;
using Markadan.Domain.Models;
using Markadan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Services;

public sealed class WishlistService : IWishlistService
{
    private readonly MarkadanDbContext _db;

    public WishlistService(MarkadanDbContext db) => _db = db;

    public Task<List<WishlistItemDTO>> GetAsync(int userId, CancellationToken ct = default)
        => _db.WishlistItems
              .AsNoTracking()
              .Where(w => w.AppUserId == userId)
              .OrderByDescending(w => w.AddedAt)
              .Select(w => new WishlistItemDTO(
                  w.Id,
                  w.ProductId,
                  w.Product.Title,
                  w.Product.Price,
                  w.Product.ImageUrl,
                  w.AddedAt))
              .ToListAsync(ct);

    public async Task<WishlistItemDTO> AddAsync(int userId, int productId, CancellationToken ct = default)
    {
        var product = await _db.Products.FindAsync([productId], ct)
            ?? throw new BusinessRuleException("Ürün bulunamadı.");

        var already = await _db.WishlistItems
            .AnyAsync(w => w.AppUserId == userId && w.ProductId == productId, ct);

        if (already)
            throw new BusinessRuleException("Bu ürün zaten favorilerinizde.");

        var item = new WishlistItem
        {
            AppUserId = userId,
            AppUser   = null!,   // EF FK yeterli
            ProductId = productId,
            Product   = product,
            AddedAt   = DateTime.UtcNow
        };
        _db.WishlistItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return new WishlistItemDTO(item.Id, productId, product.Title, product.Price, product.ImageUrl, item.AddedAt);
    }

    public async Task<bool> RemoveAsync(int userId, int wishlistItemId, CancellationToken ct = default)
    {
        var deleted = await _db.WishlistItems
            .Where(w => w.Id == wishlistItemId && w.AppUserId == userId)
            .ExecuteDeleteAsync(ct);
        return deleted > 0;
    }
}
