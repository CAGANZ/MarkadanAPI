using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class Product_Cfg : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> b)
        {
            b.ToTable("Products");
            b.HasKey(x => x.Id);

            b.Property(x => x.Title)
             .IsRequired()
             .HasMaxLength(200);

            b.Property(x => x.Price)
             .HasColumnType("decimal(18,2)")
             .IsRequired();

            b.Property(x => x.ImageUrl)
             .HasMaxLength(1024);

            b.Property(x => x.Description)
             .HasMaxLength(2000);

            // İlişkiler (bire-çok):
            b.HasOne(x => x.Brand)
             .WithMany(bd => bd.Products)
             .HasForeignKey(x => x.BrandId)
             .OnDelete(DeleteBehavior.Restrict); // bunu sor...

            b.HasOne(x => x.Category)
             .WithMany(ct => ct.Products)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            // Hafif arama/sıralama için indeksler:
            b.HasIndex(x => x.Title);
            b.HasIndex(x => x.BrandId);
            b.HasIndex(x => x.CategoryId);
            b.HasIndex(x => x.Price);
        }
    }
}
