using Microsoft.EntityFrameworkCore;

namespace IdempotentMigrationScripts.Data;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    // Added in the 2nd migration. Nullable on purpose: old app versions that don't know this
    // column can still insert customers while servers are being upgraded one by one.
    public string? Email { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().Property(customer => customer.Name).HasMaxLength(100);
        modelBuilder.Entity<Order>().Property(order => order.Total).HasPrecision(18, 2);
    }
}
