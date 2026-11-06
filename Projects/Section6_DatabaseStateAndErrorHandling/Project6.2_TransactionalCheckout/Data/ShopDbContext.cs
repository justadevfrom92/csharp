using Microsoft.EntityFrameworkCore;
using TransactionalCheckout.Data.Entities;

namespace TransactionalCheckout.Data;

public class ShopDbContext : DbContext
{
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=shop.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite has no decimal type; store as text so no cents are lost.
        modelBuilder.Entity<Product>().Property(product => product.Price).HasConversion<string>();
        modelBuilder.Entity<Customer>().Property(customer => customer.WalletBalance).HasConversion<string>();
        modelBuilder.Entity<Order>().Property(order => order.Total).HasConversion<string>();
        modelBuilder.Entity<OrderLine>().Property(line => line.UnitPrice).HasConversion<string>();

        // GLOBAL QUERY FILTER: every query on Products automatically adds "WHERE IsDeleted = 0".
        modelBuilder.Entity<Product>().HasQueryFilter(product => !product.IsDeleted);

        // SEED DATA
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Keyboard", Price = 25.00m, Stock = 10 },
            new Product { Id = 2, Name = "Mouse", Price = 12.50m, Stock = 2 },
            new Product { Id = 3, Name = "Old Webcam", Price = 40.00m, Stock = 5 });
        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, Name = "Alice", WalletBalance = 100.00m });
    }

    // Turn "delete" into "mark as deleted" for products.
    public override int SaveChanges()
    {
        foreach (var entry in ChangeTracker.Entries<Product>().Where(entry => entry.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = DateTime.UtcNow;
        }
        return base.SaveChanges();
    }
}
