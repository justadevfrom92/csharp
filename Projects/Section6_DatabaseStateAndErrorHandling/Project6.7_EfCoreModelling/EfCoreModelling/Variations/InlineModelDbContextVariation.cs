using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.Variations;

// The SAME model, written inline in OnModelCreating (no configuration classes) - fine for a few entities, hard to
// read with fifty. Deliberately WITHOUT AutoInclude: invoices load with NO lines unless the query says Include.
public class InlineModelDbContextVariation(DbContextOptions<InlineModelDbContextVariation> options) : DbContext(options)
{
    public DbSet<InvoiceModel> Invoices
    {
        get
        {
            return Set<InvoiceModel>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductModel>().Property<int>("Id").ValueGeneratedOnAdd();
        modelBuilder.Entity<ProductModel>().HasKey("Id");
        modelBuilder.Entity<ProductModel>()
            .HasDiscriminator<string>("Type")
            .HasValue<MaterialModel>("Material")
            .HasValue<ServiceModel>("Service");

        modelBuilder.Entity<InvoiceLineModel>().Property<int>("Id").ValueGeneratedOnAdd();
        modelBuilder.Entity<InvoiceLineModel>().HasKey("Id");

        modelBuilder.Entity<InvoiceModel>().Property<int>("Id").ValueGeneratedOnAdd();
        modelBuilder.Entity<InvoiceModel>().HasKey("Id");
        modelBuilder.Entity<InvoiceModel>().Ignore(invoice => invoice.Lines);
        modelBuilder.Entity<InvoiceModel>()
            .HasMany<InvoiceLineModel>("linesCollection")
            .WithOne()
            .HasForeignKey("InvoiceId");
    }
}
