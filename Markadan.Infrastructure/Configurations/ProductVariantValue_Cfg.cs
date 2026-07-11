using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class ProductVariantValue_Cfg : IEntityTypeConfiguration<ProductVariantValue>
    {
        public void Configure(EntityTypeBuilder<ProductVariantValue> b)
        {
            b.ToTable("ProductVariantValues");
            // Bileşik PK: bir varyant aynı değeri iki kez taşıyamaz
            b.HasKey(x => new { x.VariantId, x.OptionValueId });

            b.HasOne(x => x.Variant)
             .WithMany(v => v.SelectedValues)
             .HasForeignKey(x => x.VariantId)
             .OnDelete(DeleteBehavior.Cascade);

            // Restrict: bir option value bir varyantta kullanılıyorsa silinemez
            b.HasOne(x => x.OptionValue)
             .WithMany(ov => ov.VariantValues)
             .HasForeignKey(x => x.OptionValueId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => x.OptionValueId);
        }
    }
}
