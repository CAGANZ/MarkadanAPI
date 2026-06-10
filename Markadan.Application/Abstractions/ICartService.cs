using Markadan.Application.DTOs.Carts;

namespace Markadan.Application.Abstractions;

public interface ICartService
{
    // Active sepeti getirir; yoksa otomatik oluşturur
    Task<CartDTO> GetActiveCartAsync(int userId, CancellationToken ct = default);

    // Ürün ekler; aynı ürün zaten varsa miktarı artırır
    Task<CartDTO> AddItemAsync(int userId, AddCartItemDTO dto, CancellationToken ct = default);

    // Miktarı günceller; quantity = 0 ise satırı siler
    Task<CartDTO> UpdateItemQuantityAsync(int userId, int cartItemId, int quantity, CancellationToken ct = default);

    // Tek satırı siler
    Task<CartDTO> RemoveItemAsync(int userId, int cartItemId, CancellationToken ct = default);

    // Tüm satırları temizler (sepet kalır, boş olur)
    Task<CartDTO> ClearAsync(int userId, CancellationToken ct = default);
}
