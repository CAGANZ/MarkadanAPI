using Markadan.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Markadan.Infrastructure.Data
{
    public class MarkadanDbContext : IdentityDbContext<AppUser, AppRole, int>
    {
        //public MarkadanDbContext() { }
        public MarkadanDbContext(DbContextOptions<MarkadanDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Brand> Brands => Set<Brand>(); 
        public DbSet<Category> Categories => Set<Category>();
        public  DbSet<Cart> Carts  => Set<Cart>();
        public  DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Address> Addresses => Set<Address>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarkadanDbContext).Assembly);// buna geçiş yapacağım çünkü entity configuration class larını tek tek eklemek yerine assembly den alıyor.
            
        }
    }
}
