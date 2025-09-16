using Markadan.Application.DTOs.Products;

namespace Markadan.Application.Abstractions
{
    public interface IProductCommandService
    {
        // Create: yeni ürün ekler, detay DTO dönerek UI'nin direkt kullanmasını sağlar
        Task<ProductDetailDTO> CreateAsync(ProductCreateDTO dto, CancellationToken ct = default);

        // Update: kısmi güncelleme (ProductUpdateDTO alanları nullable)
        // Bulunamazsa null; controller 404 döner
        Task<ProductDetailDTO?> UpdateAsync(ProductUpdateDTO dto, CancellationToken ct = default);

        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
