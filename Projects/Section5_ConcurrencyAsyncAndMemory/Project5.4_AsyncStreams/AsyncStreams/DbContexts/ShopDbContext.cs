using AsyncStreams.Models;
using Microsoft.EntityFrameworkCore;

namespace AsyncStreams.DbContexts;

public class ShopDbContext : DbContext
{
    public ShopDbContext(DbContextOptions<ShopDbContext> options) : base(options)
    {
    }

    public DbSet<OrderModel> Orders
    {
        get
        {
            return Set<OrderModel>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite can't ORDER BY or compare decimal columns; store as double for this demo.
        modelBuilder.Entity<OrderModel>().Property(order => order.Total).HasConversion<double>();
        modelBuilder.Entity<OrderModel>().HasData(
            new OrderModel { Id = 1, Customer = "Ann", Total = 25m },
            new OrderModel { Id = 2, Customer = "Bob", Total = 300m },
            new OrderModel { Id = 3, Customer = "Cy", Total = 120m },
            new OrderModel { Id = 4, Customer = "Di", Total = 5m },
            new OrderModel { Id = 5, Customer = "Ed", Total = 999m });
    }
}
