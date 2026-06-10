using Markadan.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Markadan.Infrastructure.Data
{
    public class MarkadanDbContext : IdentityDbContext<AppUser, AppRole, int>
    {
        public MarkadanDbContext(DbContextOptions<MarkadanDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarkadanDbContext).Assembly);
        }
    }

    // Yalnızca `dotnet ef migrations` gibi design-time araçları için kullanılır.
    // Env değişkeni yoksa migration komutları çalışmaz — bu kasıtlıdır.
    public sealed class MarkadanDbContextFactory : IDesignTimeDbContextFactory<MarkadanDbContext>
    {
        public MarkadanDbContext CreateDbContext(string[] args)
        {
            var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Design-time bağlantısı için ConnectionStrings__DefaultConnection ortam değişkenini ayarlayın.");

            var optionsBuilder = new DbContextOptionsBuilder<MarkadanDbContext>();
            optionsBuilder.UseSqlServer(cs, sql =>
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null));

            return new MarkadanDbContext(optionsBuilder.Options);
        }
    }
}
