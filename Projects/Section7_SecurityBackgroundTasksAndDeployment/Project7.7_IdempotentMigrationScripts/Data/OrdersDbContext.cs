using Microsoft.EntityFrameworkCore;

namespace IdempotentMigrationScripts.Data;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().Property(customer => customer.Name).HasMaxLength(100);
        modelBuilder.Entity<Order>().Property(order => order.Total).HasPrecision(18, 2);
    }
}
