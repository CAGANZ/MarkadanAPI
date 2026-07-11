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
            // (CartId, ProductId, ProductVariantId) unique: aynı ürün+varyant iki satır olamaz,
            // miktar artırılır. Basit üründe ProductVariantId null (SQL Server tek null kaydı korur).
            // HasFilter(null): EF'in nullable kolon için otomatik eklediği "IS NOT NULL" filtresini kapatır.
            // Böylece basit üründe (variantId null) de tek satır garanti edilir.
            b.HasIndex(x => new { x.CartId, x.ProductId, x.ProductVariantId }).IsUnique().HasFilter(null);

            b.HasOne(x => x.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict: siparişte geçen ürün silinemez
            b.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict: siparişte geçen varyant silinemez
            b.HasOne(x => x.ProductVariant)
                .WithMany()
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
