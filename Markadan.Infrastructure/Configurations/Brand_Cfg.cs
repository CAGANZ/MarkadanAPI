using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class Brand_Cfg : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> b)
        {
            b.ToTable("Brands");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name)
             .IsRequired()
             .HasMaxLength(150)
             .UseCollation("Turkish_100_CI_AI");

            b.HasIndex(x => x.Name).IsUnique();

            b.Property(x => x.ImageUrl)
             .HasMaxLength(1024);

            b.Property(x => x.Description)
             .HasMaxLength(2000);

            b.HasIndex(x => x.Name).IsUnique(false);
        }
    }
}
