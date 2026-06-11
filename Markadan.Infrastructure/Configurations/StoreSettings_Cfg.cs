using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations;

public class StoreSettings_Cfg : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> b)
    {
        b.ToTable("StoreSettings");
        b.HasKey(x => x.Id);

        b.Property(x => x.StoreName).IsRequired().HasMaxLength(100);
        b.Property(x => x.LogoUrl).HasMaxLength(500);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.WhatsAppPhone).HasMaxLength(20);
        b.Property(x => x.ContactPhone).HasMaxLength(20);
        b.Property(x => x.ContactEmail).HasMaxLength(200);
        b.Property(x => x.InstagramUrl).HasMaxLength(200);
        b.Property(x => x.FacebookUrl).HasMaxLength(200);
        b.Property(x => x.PrimaryColor).HasMaxLength(7);   // #RRGGBB
        b.Property(x => x.AccentColor).HasMaxLength(7);
        b.Property(x => x.MetaDescription).HasMaxLength(300);
    }
}
