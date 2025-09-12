using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class Address_Cfg : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> b)
        {
            b.ToTable("Addresses");
            b.HasKey(x => x.Id);

            b.Property(x => x.AddressName)
                .IsRequired()
                .HasMaxLength(100);

            b.Property(x => x.Street)
                .HasMaxLength(200);

            b.Property(x => x.City)
                .HasMaxLength(100);

            b.Property(x => x.State)
                .HasMaxLength(100);

            b.Property(x => x.PostalCode)
                .HasMaxLength(20);

            b.Property(x => x.Country)
                .HasMaxLength(100);

            // User ilişki
            b.HasOne(x => x.AppUser)
                .WithMany(u => u.Addresses)   // AppUser tarafında ICollection<Address> tanımlaman lazım
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index: kullanıcı + şehir (sorgu hızlandırma için)
            b.HasIndex(x => new { x.AppUserId, x.City });
        }
    }
}
