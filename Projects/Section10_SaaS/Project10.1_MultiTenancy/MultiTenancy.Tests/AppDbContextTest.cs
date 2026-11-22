using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MultiTenancy.Contexts;
using MultiTenancy.DbContexts;
using MultiTenancy.Models;
using MultiTenancy.Services;

namespace MultiTenancy.Tests;

// The two safety nets in AppDbContext: the global query filter (reads) and tenant stamping (writes).
public class AppDbContextTest : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public AppDbContextTest()
    {
        connection.Open();
        using AppDbContext setup = For(null);
        setup.Database.EnsureCreated();
    }

    public void Dispose()
    {
        connection.Dispose();
    }

    private AppDbContext For(string? tenantId)
    {
        var tenantContext = new TenantContext();
        if (tenantId != null)
        {
            tenantContext.Set(new TenantModel(tenantId, tenantId));
        }
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options, tenantContext);
    }

    [Fact]
    public async Task Reads_PositivePositive_EachTenantSeesOnlyItsOwnRows()
    {
        using AppDbContext acme = For("acme");
        using AppDbContext globex = For("globex");

        Assert.Equal([1, 2], (await new ProjectService(acme).ListAsync()).Select(project => project.Id));
        Assert.Equal([3], (await new ProjectService(globex).ListAsync()).Select(project => project.Id));
    }

    [Fact]
    public async Task Reads_PositiveNegative_NoTenantSeesNothing_AdminSeesAll_FromSqlIsStillFiltered()
    {
        using AppDbContext noTenant = For(null);
        using AppDbContext acme = For("acme");

        Assert.Empty(await noTenant.Projects.ToListAsync());                                  // forgot to set a tenant -> nothing, not everything
        Assert.Equal(3, await new ProjectService(acme).CountAllTenantsForAdminAsync());     // IgnoreQueryFilters
        Assert.Equal(2, await acme.Projects.FromSql($"SELECT * FROM Projects").CountAsync()); // the filter wraps raw SQL too
    }

    [Fact]
    public async Task Reads_NegativeNegative_OtherTenantsIds_LookMissing_AndRawSqlCommandsBypassTheFilter()
    {
        using AppDbContext acme = For("acme");
        var service = new ProjectService(acme);

        Assert.Null(await service.FindAsync(3));                       // globex's project
        Assert.False(await service.RenameAsync(3, "Hacked"));
        Assert.Equal(0, await service.DeleteAsync(3));                 // ExecuteDelete is filtered too
        Assert.Equal(1, await acme.Database.ExecuteSqlAsync($"UPDATE Projects SET Name = 'Raw' WHERE Id = 3")); // NOT filtered: raw commands bypass the net
        using AppDbContext globex = For("globex");
        Assert.Equal("Raw", (await globex.Projects.SingleAsync()).Name);
    }

    [Fact]
    public async Task Writes_PositivePositive_NewRowsGetTheCurrentTenant()
    {
        using AppDbContext acme = For("acme");

        ProjectModel created = await new ProjectService(acme).CreateAsync("Shop");

        Assert.Equal("acme", created.TenantId);
    }

    [Fact]
    public async Task Writes_PositiveNegative_TenantIdSetByCode_IsOverwritten()
    {
        using AppDbContext acme = For("acme");
        acme.Projects.Add(new ProjectModel { Name = "Sneaky", TenantId = "globex" });

        await acme.SaveChangesAsync();

        using AppDbContext globex = For("globex");
        Assert.DoesNotContain("Sneaky", await globex.Projects.Select(project => project.Name).ToListAsync());
    }

    [Fact]
    public async Task Writes_NegativeNegative_NoTenant_MovingARow_OrEditingAnotherTenantsRow_Throw()
    {
        using AppDbContext noTenant = For(null);
        noTenant.Projects.Add(new ProjectModel { Name = "Orphan" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => noTenant.SaveChangesAsync());

        using AppDbContext acme = For("acme");
        ProjectModel own = await acme.Projects.FirstAsync();
        own.TenantId = "globex";
        await Assert.ThrowsAsync<InvalidOperationException>(() => acme.SaveChangesAsync()); // moving a row to another tenant

        using AppDbContext acme2 = For("acme");
        ProjectModel globexRow = await acme2.Projects.IgnoreQueryFilters().SingleAsync(project => project.Id == 3);
        globexRow.Name = "Hacked";
        await Assert.ThrowsAsync<InvalidOperationException>(() => acme2.SaveChangesAsync()); // even if it got loaded somehow
    }

    [Fact]
    public void TenantContext_NegativeNegative_SwitchingTenantMidRequest_Throws()
    {
        var tenantContext = new TenantContext();
        tenantContext.Set(new TenantModel("acme", "Acme Ltd"));

        tenantContext.Set(new TenantModel("acme", "Acme Ltd")); // same tenant again is fine
        Assert.Throws<InvalidOperationException>(() => tenantContext.Set(new TenantModel("globex", "Globex Corp")));
    }
}
