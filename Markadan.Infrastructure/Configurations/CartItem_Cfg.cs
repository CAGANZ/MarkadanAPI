using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class CartItem_Cfg : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> b)
        {
            b.ToTable("CartItems");
            b.HasKey(x => x.Id);

            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.UnitPriceSnapshot).HasColumnType("decimal(18,2)");

            b.HasIndex(x => x.CartId);
            b.HasIndex(x => x.ProductId);

            b.HasOne(x => x.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
