using Microsoft.EntityFrameworkCore;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal WalletBalance { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal Total { get; set; }
    public List<OrderLine> Lines { get; set; } = new();
}

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } // copied at checkout, so later price changes don't change old orders
}

public class ShopDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

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
