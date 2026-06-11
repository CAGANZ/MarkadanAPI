using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations;

public class WishlistItem_Cfg : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> b)
    {
        b.ToTable("WishlistItems");
        b.HasKey(x => x.Id);

        b.HasIndex(x => new { x.AppUserId, x.ProductId }).IsUnique();

        b.HasOne(x => x.AppUser)
         .WithMany()
         .HasForeignKey(x => x.AppUserId)
         .OnDelete(DeleteBehavior.Cascade);

        // Ürün silinince favori kaydı da gider — liste tutarlı kalır
        b.HasOne(x => x.Product)
         .WithMany()
         .HasForeignKey(x => x.ProductId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
