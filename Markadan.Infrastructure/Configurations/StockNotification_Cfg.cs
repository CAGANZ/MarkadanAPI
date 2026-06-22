using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations;

public sealed class StockNotification_Cfg : IEntityTypeConfiguration<StockNotification>
{
    public void Configure(EntityTypeBuilder<StockNotification> b)
    {
        b.ToTable("StockNotifications");
        b.HasKey(x => x.Id);

        b.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(200);

        b.Property(x => x.Name)
            .HasMaxLength(100);

        // Aynı kişi aynı ürüne iki kez kayıt olamasın
        b.HasIndex(x => new { x.ProductId, x.Email }).IsUnique();

        b.HasOne(x => x.Product)
         .WithMany()
         .HasForeignKey(x => x.ProductId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
