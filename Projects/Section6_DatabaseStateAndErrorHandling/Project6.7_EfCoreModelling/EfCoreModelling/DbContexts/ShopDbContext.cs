using EfCoreModelling.Configurations;
using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.DbContexts;

// The original: every entity's setup lives in its own configuration class, applied one by one.
public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<InvoiceModel> Invoices
    {
        get
        {
            return Set<InvoiceModel>();
        }
    }

    public DbSet<InvoiceLineModel> InvoiceLines
    {
        get
        {
            return Set<InvoiceLineModel>();
        }
    }

    public DbSet<ProductModel> Products
    {
        get
        {
            return Set<ProductModel>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceLineConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
    }
}
