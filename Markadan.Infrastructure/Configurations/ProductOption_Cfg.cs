using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class ProductOption_Cfg : IEntityTypeConfiguration<ProductOption>
    {
        public void Configure(EntityTypeBuilder<ProductOption> b)
        {
            b.ToTable("ProductOptions");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name)
             .IsRequired()
             .HasMaxLength(100)
             .UseCollation("Turkish_100_CI_AI");

            b.Property(x => x.SortOrder);

            b.HasOne(x => x.Product)
             .WithMany(p => p.Options)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade); // ürün silinince seçenekleri de gider

            b.HasIndex(x => x.ProductId);
            // Aynı üründe aynı isimli eksen olmaz
            b.HasIndex(x => new { x.ProductId, x.Name }).IsUnique();
        }
    }
}
