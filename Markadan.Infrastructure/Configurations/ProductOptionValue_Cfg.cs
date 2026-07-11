using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class ProductOptionValue_Cfg : IEntityTypeConfiguration<ProductOptionValue>
    {
        public void Configure(EntityTypeBuilder<ProductOptionValue> b)
        {
            b.ToTable("ProductOptionValues");
            b.HasKey(x => x.Id);

            b.Property(x => x.Value)
             .IsRequired()
             .HasMaxLength(100)
             .UseCollation("Turkish_100_CI_AI");

            b.Property(x => x.SortOrder);

            b.HasOne(x => x.Option)
             .WithMany(o => o.Values)
             .HasForeignKey(x => x.OptionId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.OptionId);
            // Aynı eksende aynı değer olmaz
            b.HasIndex(x => new { x.OptionId, x.Value }).IsUnique();
        }
    }
}
