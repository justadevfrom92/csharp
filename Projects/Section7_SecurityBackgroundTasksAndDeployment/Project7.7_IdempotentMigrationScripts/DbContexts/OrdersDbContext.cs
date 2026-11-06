using IdempotentMigrationScripts.Models;
using Microsoft.EntityFrameworkCore;

namespace IdempotentMigrationScripts.DbContexts;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    public DbSet<CustomerModel> Customers { get; set; } = null!;
    public DbSet<OrderModel> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerModel>().Property(customer => customer.Name).HasMaxLength(100);
        modelBuilder.Entity<OrderModel>().Property(order => order.Total).HasPrecision(18, 2);
    }
}
