using EfCoreAdvancedQueries.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreAdvancedQueries.DbContexts;

public class ShopDbContext : DbContext
{
    public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options)
    {
    }

    public DbSet<ProductModel> Products { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductModel>(product =>
        {
            product.ToTable("Products");
            product.Property(p => p.Name).HasMaxLength(100).IsRequired();
            product.Property(p => p.Category).HasMaxLength(50).IsRequired();
            product.Property(p => p.Version).IsConcurrencyToken();
            product.HasData(
                new ProductModel { Id = 1, Name = "Keyboard", Category = "Computers", PriceInCents = 2500, Stock = 10 },
                new ProductModel { Id = 2, Name = "Mouse", Category = "Computers", PriceInCents = 1250, Stock = 3 },
                new ProductModel { Id = 3, Name = "Monitor", Category = "Computers", PriceInCents = 18000, Stock = 8 },
                new ProductModel { Id = 4, Name = "Desk", Category = "Furniture", PriceInCents = 22000, Stock = 2 },
                new ProductModel { Id = 5, Name = "Old Webcam", Category = "Computers", PriceInCents = 4000, Stock = 0, IsDiscontinued = true },
                new ProductModel { Id = 6, Name = "Wobbly Chair", Category = "Furniture", PriceInCents = 3000, Stock = 1, IsDiscontinued = true });
        });
    }
}
