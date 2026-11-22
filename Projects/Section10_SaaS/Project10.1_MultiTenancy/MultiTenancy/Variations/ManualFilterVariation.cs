using Microsoft.EntityFrameworkCore;
using MultiTenancy.Models;

namespace MultiTenancy.Variations;

// Filtering BY HAND in every query (no global filter). Works - until one query forgets.
public static class ManualFilterVariation
{
    public static Task<List<ProjectModel>> ListWithWhere(UnfilteredDbContextVariation database, string tenantId)
    {
        return database.Projects.Where(project => project.TenantId == tenantId).OrderBy(project => project.Id).ToListAsync();
    }

    // The bug that gets SaaS companies in the news: one forgotten Where = every customer's data.
    public static Task<List<ProjectModel>> ListForgotWhere(UnfilteredDbContextVariation database)
    {
        return database.Projects.OrderBy(project => project.Id).ToListAsync();
    }

    // Also easy to forget on writes: the client-sent TenantId is trusted.
    public static async Task<ProjectModel> CreateTrustingTheClient(UnfilteredDbContextVariation database, string name, string tenantIdFromClient)
    {
        var project = new ProjectModel { Name = name, TenantId = tenantIdFromClient };
        database.Projects.Add(project);
        await database.SaveChangesAsync();
        return project;
    }
}
