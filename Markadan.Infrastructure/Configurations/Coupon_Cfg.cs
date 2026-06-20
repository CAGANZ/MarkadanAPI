using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations;

public sealed class Coupon_Cfg : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.HasKey(c => c.Id);

        b.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        // Kod unique ve büyük harfle saklanır; sorgularda ToUpper() kullanılır
        b.HasIndex(c => c.Code).IsUnique();

        b.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(c => c.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        b.Property(c => c.Value)
            .HasColumnType("decimal(18,2)");

        b.Property(c => c.MinOrderAmount)
            .HasColumnType("decimal(18,2)");
    }
}
