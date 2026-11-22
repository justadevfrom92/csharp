using Microsoft.EntityFrameworkCore;
using MultiTenancy.DbContexts;
using MultiTenancy.Models;

namespace MultiTenancy.Services;

// Notice: NO "Where(TenantId == ...)" anywhere. The global query filter adds it, so it can't be forgotten.
public class ProjectService
{
    private readonly AppDbContext database;

    public ProjectService(AppDbContext database)
    {
        this.database = database;
    }

    public Task<List<ProjectModel>> ListAsync()
    {
        return database.Projects.OrderBy(project => project.Id).ToListAsync();
    }

    // Another tenant's id gives null - the same as "doesn't exist", so ids from other tenants can't be probed.
    public Task<ProjectModel?> FindAsync(int id)
    {
        return database.Projects.FirstOrDefaultAsync(project => project.Id == id);
    }

    public async Task<ProjectModel> CreateAsync(string name)
    {
        var project = new ProjectModel { Name = name }; // TenantId filled in by SaveChanges
        database.Projects.Add(project);
        await database.SaveChangesAsync();
        return project;
    }

    public async Task<bool> RenameAsync(int id, string name)
    {
        ProjectModel? project = await FindAsync(id);
        if (project == null)
        {
            return false;
        }
        project.Name = name;
        await database.SaveChangesAsync();
        return true;
    }

    // ExecuteDelete also gets the filter: it deletes only the CURRENT tenant's matching rows.
    public Task<int> DeleteAsync(int id)
    {
        return database.Projects.Where(project => project.Id == id).ExecuteDeleteAsync();
    }

    // For the company's OWN staff (support, billing): IgnoreQueryFilters() turns the safety net off.
    // Keep calls like this few, named clearly, and behind an admin-only policy.
    public Task<int> CountAllTenantsForAdminAsync()
    {
        return database.Projects.IgnoreQueryFilters().CountAsync();
    }
}
