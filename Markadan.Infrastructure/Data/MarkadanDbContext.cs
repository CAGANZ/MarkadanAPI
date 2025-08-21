using Markadan.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Data
{
    public class MarkadanDbContext : DbContext
    {
        public MarkadanDbContext() { }
        public MarkadanDbContext(DbContextOptions<MarkadanDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var cs =
                    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("MARKADAN__CS")
                    ?? "Server=CAGANZ;Database=MarkadanDb;Trusted_Connection=True;TrustServerCertificate=True;";

                optionsBuilder.UseSqlServer(cs, sql =>
                    sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Brand
            modelBuilder.Entity<Brand>(b =>
            {
                b.ToTable("Brands");
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired().HasMaxLength(100);
                b.HasIndex(x => x.Name).IsUnique(false);
            });

            // Category
            modelBuilder.Entity<Category>(c =>
            {
                c.ToTable("Categories");
                c.HasKey(x => x.Id);
                c.Property(x => x.Name).IsRequired().HasMaxLength(100);
                c.HasIndex(x => x.Name).IsUnique(false);
            });

            // Product
            modelBuilder.Entity<Product>(p =>
            {
                p.ToTable("Products");
                p.HasKey(x => x.Id);

                p.Property(x => x.Title).IsRequired().HasMaxLength(200);
                p.Property(x => x.Price).HasColumnType("decimal(18,2)");
                p.Property(x => x.Stock).IsRequired();
                p.Property(x => x.ImageUrl).HasMaxLength(2048);

                p.HasIndex(x => x.Title);
                p.HasIndex(x => new { x.CategoryId, x.BrandId });

                p.HasOne(x => x.Brand)
                 .WithMany(b => b.Products)
                 .HasForeignKey(x => x.BrandId)
                 .OnDelete(DeleteBehavior.Restrict);

                p.HasOne(x => x.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(x => x.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });


        }
    }
}
