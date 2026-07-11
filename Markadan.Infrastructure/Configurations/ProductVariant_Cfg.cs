using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class ProductVariant_Cfg : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> b)
        {
            b.ToTable("ProductVariants");
            b.HasKey(x => x.Id);

            b.Property(x => x.Sku).HasMaxLength(100);
            b.Property(x => x.Price).HasColumnType("decimal(18,2)"); // nullable — null ise ürün fiyatı geçerli
            b.Property(x => x.Stock).IsRequired();
            b.Property(x => x.ImageUrl).HasMaxLength(1024);
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasOne(x => x.Product)
             .WithMany(p => p.Variants)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.ProductId);
        }
    }
}
