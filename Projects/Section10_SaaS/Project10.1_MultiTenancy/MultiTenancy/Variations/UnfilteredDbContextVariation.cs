using Microsoft.EntityFrameworkCore;
using MultiTenancy.Models;

namespace MultiTenancy.Variations;

// The same table with NO global query filter and NO tenant stamping: every query must remember
// ".Where(project => project.TenantId == ...)" itself (ManualFilterVariation).
public class UnfilteredDbContextVariation : DbContext
{
    public UnfilteredDbContextVariation(DbContextOptions<UnfilteredDbContextVariation> options) : base(options)
    {
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
        modelBuilder.Entity<ProjectModel>().HasData(
            new ProjectModel { Id = 1, TenantId = "acme", Name = "Website" },
            new ProjectModel { Id = 2, TenantId = "acme", Name = "Mobile app" },
            new ProjectModel { Id = 3, TenantId = "globex", Name = "Rocket" });
    }
}
