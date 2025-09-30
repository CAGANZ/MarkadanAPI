using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Data.Config
{
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> b)
        {
            b.ToTable("RefreshTokens");
            b.HasKey(x => x.Id);

            b.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.ExpiresAtUtc).IsRequired();

            b.HasIndex(x => x.Token).IsUnique();

            b.HasOne(x => x.AppUser)
             .WithMany()
             .HasForeignKey(x => x.AppUserId)
             .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
