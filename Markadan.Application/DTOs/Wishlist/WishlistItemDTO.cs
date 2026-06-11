namespace Markadan.Application.DTOs.Wishlist;

public record WishlistItemDTO(
    int Id,
    int ProductId,
    string ProductTitle,
    decimal ProductPrice,
    string? ProductImageUrl,
    DateTime AddedAt
);
