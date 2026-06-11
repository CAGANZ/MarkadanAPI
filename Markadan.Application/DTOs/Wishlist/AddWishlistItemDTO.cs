using System.ComponentModel.DataAnnotations;

namespace Markadan.Application.DTOs.Wishlist;

public record AddWishlistItemDTO([Required] int ProductId);
