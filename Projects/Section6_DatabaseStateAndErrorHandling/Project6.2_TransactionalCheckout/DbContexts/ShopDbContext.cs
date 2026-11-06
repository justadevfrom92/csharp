using Microsoft.EntityFrameworkCore;
using TransactionalCheckout.Models;

namespace TransactionalCheckout.DbContexts;

public class ShopDbContext : DbContext
{
    public DbSet<ProductModel> Products { get; set; } = null!;
    public DbSet<CustomerModel> Customers { get; set; } = null!;
    public DbSet<OrderModel> Orders { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=shop.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite has no decimal type; store as text so no cents are lost.
        modelBuilder.Entity<ProductModel>().Property(product => product.Price).HasConversion<string>();
        modelBuilder.Entity<CustomerModel>().Property(customer => customer.WalletBalance).HasConversion<string>();
        modelBuilder.Entity<OrderModel>().Property(order => order.Total).HasConversion<string>();
        modelBuilder.Entity<OrderLineModel>().Property(line => line.UnitPrice).HasConversion<string>();

        // OrderLineModel has no DbSet, so EF would name its table "OrderLineModel" - keep the table name "OrderLine".
        modelBuilder.Entity<OrderLineModel>().ToTable("OrderLine");

        // GLOBAL QUERY FILTER: every query on Products automatically adds "WHERE IsDeleted = 0".
        modelBuilder.Entity<ProductModel>().HasQueryFilter(product => !product.IsDeleted);

        // SEED DATA
        modelBuilder.Entity<ProductModel>().HasData(
            new ProductModel { Id = 1, Name = "Keyboard", Price = 25.00m, Stock = 10 },
            new ProductModel { Id = 2, Name = "Mouse", Price = 12.50m, Stock = 2 },
            new ProductModel { Id = 3, Name = "Old Webcam", Price = 40.00m, Stock = 5 });
        modelBuilder.Entity<CustomerModel>().HasData(
            new CustomerModel { Id = 1, Name = "Alice", WalletBalance = 100.00m });
    }

    // Turn "delete" into "mark as deleted" for products.
    public override int SaveChanges()
    {
        foreach (var entry in ChangeTracker.Entries<ProductModel>().Where(entry => entry.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = DateTime.UtcNow;
        }
        return base.SaveChanges();
    }
}
