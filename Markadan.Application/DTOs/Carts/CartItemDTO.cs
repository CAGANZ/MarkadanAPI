namespace Markadan.Application.DTOs.Carts;

public record CartItemDTO(
    int Id,                     // CartItem.Id — PUT/DELETE için gerekli
    int ProductId,
    string Title,               // Product.Title (canlı)
    string? ImageUrl,           // Product.ImageUrl (canlı)
    decimal UnitPriceSnapshot,  // sepete eklendiğindeki fiyat
    decimal CurrentPrice,       // Product.Price (şu anki fiyat)
    bool PriceChanged,          // frontend uyarı gösterir
    int Quantity,
    decimal Subtotal            // UnitPriceSnapshot * Quantity
);
