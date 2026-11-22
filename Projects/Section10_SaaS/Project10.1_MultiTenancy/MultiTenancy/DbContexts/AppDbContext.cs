using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MultiTenancy.Contexts;
using MultiTenancy.Models;

namespace MultiTenancy.DbContexts;

// ONE database shared by all tenants; every row has a TenantId. Two safety nets:
//   1. READS: a GLOBAL QUERY FILTER adds "WHERE TenantId = <current tenant>" to EVERY query, automatically.
//   2. WRITES: SaveChanges stamps new rows with the current tenant and refuses to change another tenant's rows.
public class AppDbContext : DbContext
{
    private readonly TenantContext tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, TenantContext tenantContext) : base(options)
    {
        this.tenantContext = tenantContext;
    }

    // The filter must read a property OF THE DbContext: EF re-reads it for every query.
    // (A plain local variable would be baked into the cached model once - for the first tenant only.)
    public string CurrentTenantId
    {
        get
        {
            return tenantContext.TenantId;
        }
    }

    public DbSet<ProjectModel> Projects
    {
        get
        {
            return Set<ProjectModel>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectModel>().HasQueryFilter(project => project.TenantId == CurrentTenantId);
        modelBuilder.Entity<ProjectModel>().HasIndex(project => project.TenantId); // every query filters on it
        modelBuilder.Entity<ProjectModel>().HasData(
            new ProjectModel { Id = 1, TenantId = "acme", Name = "Website" },
            new ProjectModel { Id = 2, TenantId = "acme", Name = "Mobile app" },
            new ProjectModel { Id = 3, TenantId = "globex", Name = "Rocket" });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampTenant()
    {
        foreach (EntityEntry<ProjectModel> entry in ChangeTracker.Entries<ProjectModel>())
        {
            if (entry.State == EntityState.Unchanged || entry.State == EntityState.Detached)
            {
                continue;
            }
            if (CurrentTenantId == "")
            {
                throw new InvalidOperationException("No tenant set: refusing to save.");
            }
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = CurrentTenantId;
            }
            else if (entry.Entity.TenantId != CurrentTenantId || entry.Property(project => project.TenantId).IsModified)
            {
                throw new InvalidOperationException("Changing another tenant's data is not allowed.");
            }
        }
    }
}
