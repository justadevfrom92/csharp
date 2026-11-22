using Microsoft.EntityFrameworkCore;
using MultiTenancy.Contexts;
using MultiTenancy.Models;

namespace MultiTenancy.Variations;

// The filter WRITTEN WRONG: the tenant id is copied into a local variable inside OnModelCreating.
// EF builds the model ONCE per DbContext type and caches it, so the FIRST tenant's id is baked in forever:
// every later tenant sees the first tenant's data. AppDbContext (original) reads a property of the DbContext instead.
public class CapturedFilterDbContextVariation : DbContext
{
    private readonly TenantContext tenantContext;

    public CapturedFilterDbContextVariation(DbContextOptions<CapturedFilterDbContextVariation> options, TenantContext tenantContext) : base(options)
    {
        this.tenantContext = tenantContext;
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
        string tenantId = tenantContext.TenantId; // runs once, for whoever is first
        modelBuilder.Entity<ProjectModel>().HasQueryFilter(project => project.TenantId == tenantId);
        modelBuilder.Entity<ProjectModel>().HasData(
            new ProjectModel { Id = 1, TenantId = "acme", Name = "Website" },
            new ProjectModel { Id = 2, TenantId = "acme", Name = "Mobile app" },
            new ProjectModel { Id = 3, TenantId = "globex", Name = "Rocket" });
    }
}
