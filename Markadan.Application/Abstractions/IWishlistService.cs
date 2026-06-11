using Markadan.Application.DTOs.Wishlist;

namespace Markadan.Application.Abstractions;

public interface IWishlistService
{
    Task<List<WishlistItemDTO>> GetAsync(int userId, CancellationToken ct = default);
    Task<WishlistItemDTO> AddAsync(int userId, int productId, CancellationToken ct = default);
    Task<bool> RemoveAsync(int userId, int wishlistItemId, CancellationToken ct = default);
}
