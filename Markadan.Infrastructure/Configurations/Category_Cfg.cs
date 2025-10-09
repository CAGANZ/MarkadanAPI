using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Markadan.Infrastructure.Configurations
{
    public class Category_Cfg : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> b)
        {
            b.ToTable("Categories");
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
