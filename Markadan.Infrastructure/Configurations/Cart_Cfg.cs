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

            b.HasOne(x => x.AppUser)
             .WithMany(u => u.Carts)
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
