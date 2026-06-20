using Markadan.Application.DTOs.Carts;
using Markadan.Application.DTOs.Coupons;

namespace Markadan.Application.Abstractions;

public interface ICouponService
{
    // Kullanıcı uçları
    Task<CartDTO> ApplyCouponAsync(int userId, string code, CancellationToken ct = default);
    Task<CartDTO> RemoveCouponAsync(int userId, CancellationToken ct = default);

    // Admin uçları
    Task<IReadOnlyList<CouponDTO>> GetAllAsync(CancellationToken ct = default);
    Task<CouponDTO> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CouponDTO> CreateAsync(CreateCouponDTO dto, CancellationToken ct = default);
    Task<CouponDTO> UpdateAsync(int id, UpdateCouponDTO dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
