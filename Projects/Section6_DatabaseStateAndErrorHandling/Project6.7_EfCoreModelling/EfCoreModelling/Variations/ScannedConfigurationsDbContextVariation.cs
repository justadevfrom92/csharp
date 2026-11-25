using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.Variations;

// ApplyConfigurationsFromAssembly: finds EVERY IEntityTypeConfiguration class in the assembly and applies it.
// No list to keep up to date - but a configuration class meant for another DbContext would be applied too.
public class ScannedConfigurationsDbContextVariation(DbContextOptions<ScannedConfigurationsDbContextVariation> options) : DbContext(options)
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScannedConfigurationsDbContextVariation).Assembly);
    }
}
