using EfCoreModelling.Configurations;
using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.Variations;

// TABLE-PER-TYPE instead of one table: "Products" holds the shared columns, "Materials" and "Services" hold their
// own, linked by the same Id. No Type column; every OfType/kind check becomes a JOIN.
public class TablePerTypeDbContextVariation(DbContextOptions<TablePerTypeDbContextVariation> options) : DbContext(options)
{
    public DbSet<ProductModel> Products
    {
        get
        {
            return Set<ProductModel>();
        }
    }

    public DbSet<InvoiceLineModel> InvoiceLines
    {
        get
        {
            return Set<InvoiceLineModel>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceLineConfiguration());
        modelBuilder.Entity<ProductModel>().Property<int>("Id").ValueGeneratedOnAdd();
        modelBuilder.Entity<ProductModel>().HasKey("Id");
        modelBuilder.Entity<ProductModel>().UseTptMappingStrategy();
        modelBuilder.Entity<MaterialModel>().ToTable("Materials");
        modelBuilder.Entity<ServiceModel>().ToTable("Services");
    }
}
