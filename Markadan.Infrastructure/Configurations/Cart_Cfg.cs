using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class Cart_Cfg : IEntityTypeConfiguration<Cart>
    {
        public void Configure(EntityTypeBuilder<Cart> b)
        {
            b.ToTable("Carts");
            b.HasKey(x => x.Id);

            b.Property(x => x.Status).HasConversion<byte>();
            b.Property(x => x.CreatedAt).IsRequired();

            b.Property(x => x.OrderNumber).HasMaxLength(20);
            b.HasIndex(x => x.OrderNumber).IsUnique();  // NULL birden fazla olabilir; Ordered'da benzersiz

            b.Property(x => x.AbandonedReminderSentAt);

            b.Property(x => x.IyzicoConversationId).HasMaxLength(50);
            b.Property(x => x.IyzicoPaymentId).HasMaxLength(50);
            b.Property(x => x.PaidAtUtc);

            b.Property(x => x.ShippingStreet).HasMaxLength(200);
            b.Property(x => x.ShippingCity).HasMaxLength(100);
            b.Property(x => x.ShippingState).HasMaxLength(100);
            b.Property(x => x.ShippingPostalCode).HasMaxLength(20);
            b.Property(x => x.ShippingCountry).HasMaxLength(100);

            b.HasOne(x => x.AppUser)
             .WithMany(u => u.Carts)
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Restrict);

            // SetNull: kullanıcı adresini silerse FK null olur, snapshot alanlar korunur
            b.HasOne(x => x.ShippingAddress)
             .WithMany()
             .HasForeignKey(x => x.ShippingAddressId)
             .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
