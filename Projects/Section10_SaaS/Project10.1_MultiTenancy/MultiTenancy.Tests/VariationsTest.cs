using System.Security.Claims;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MultiTenancy.Contexts;
using MultiTenancy.Models;
using MultiTenancy.Variations;

namespace MultiTenancy.Tests;

public class VariationsTest : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly UnfilteredDbContextVariation unfiltered;

    public VariationsTest()
    {
        connection.Open();
        unfiltered = new UnfilteredDbContextVariation(new DbContextOptionsBuilder<UnfilteredDbContextVariation>().UseSqlite(connection).Options);
        unfiltered.Database.EnsureCreated();
    }

    public void Dispose()
    {
        unfiltered.Dispose();
        connection.Dispose();
    }

    [Fact]
    public async Task ManualFilter_PositivePositive_WhereByHand_Works()
    {
        Assert.Equal([1, 2], (await ManualFilterVariation.ListWithWhere(unfiltered, "acme")).Select(project => project.Id));
    }

    [Fact]
    public async Task ManualFilter_PositiveNegative_OneForgottenWhere_LeaksEveryTenant()
    {
        List<ProjectModel> all = await ManualFilterVariation.ListForgotWhere(unfiltered);

        Assert.Equal(["acme", "globex"], all.Select(project => project.TenantId).Distinct().Order());
    }

    [Fact]
    public async Task ManualFilter_NegativeNegative_TrustingTheClientsTenantId_WritesIntoAnotherTenant()
    {
        await ManualFilterVariation.CreateTrustingTheClient(unfiltered, "Planted", "globex"); // sent by an acme user

        Assert.Contains(await ManualFilterVariation.ListWithWhere(unfiltered, "globex"), project => project.Name == "Planted");
    }

    [Fact]
    public async Task CapturedFilter_PositiveNegative_FirstTenantIsBakedIn_SecondTenantSeesTheFirstTenantsData()
    {
        static CapturedFilterDbContextVariation For(SqliteConnection connection, string tenantId)
        {
            var tenantContext = new TenantContext();
            tenantContext.Set(new TenantModel(tenantId, tenantId));
            return new CapturedFilterDbContextVariation(new DbContextOptionsBuilder<CapturedFilterDbContextVariation>().UseSqlite(connection).Options, tenantContext);
        }
        using CapturedFilterDbContextVariation acme = For(connection, "acme");
        List<string> acmeSees = await acme.Projects.Select(project => project.TenantId).ToListAsync(); // model built now, "acme" baked in
        using CapturedFilterDbContextVariation globex = For(connection, "globex");

        List<string> globexSees = await globex.Projects.Select(project => project.TenantId).ToListAsync();

        Assert.Equal(["acme", "acme"], acmeSees);
        Assert.Equal(["acme", "acme"], globexSees); // LEAK: globex gets acme's rows
    }

    [Fact]
    public async Task DatabasePerTenant_PositivePositive_EachTenantHasItsOwnRows_NoFilterNeeded()
    {
        using var databases = new DatabasePerTenantVariation();
        using (UnfilteredDbContextVariation acme = databases.CreateFor("acme"))
        {
            acme.Projects.Add(new ProjectModel { Name = "Website", TenantId = "acme" });
            await acme.SaveChangesAsync();
        }

        using UnfilteredDbContextVariation acmeAgain = databases.CreateFor("acme");
        using UnfilteredDbContextVariation globex = databases.CreateFor("globex");

        Assert.Equal(["Website"], await acmeAgain.Projects.Select(project => project.Name).ToListAsync());
        Assert.Empty(await globex.Projects.ToListAsync()); // even "forgot the Where" can't leak
    }

    [Fact]
    public async Task DatabasePerTenant_PositiveNegative_ANewTenantStartsEmpty()
    {
        using var databases = new DatabasePerTenantVariation();
        using UnfilteredDbContextVariation initech = databases.CreateFor("initech");

        Assert.Empty(await initech.Projects.ToListAsync());
    }

    [Fact]
    public void DatabasePerTenant_NegativeNegative_BlankTenant_Throws()
    {
        using var databases = new DatabasePerTenantVariation();

        Assert.Throws<ArgumentException>(() => databases.CreateFor(" "));
    }

    [Fact]
    public void Lookup_PositivePositive_HostClaimAndPath()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant", "acme")], authenticationType: "Bearer"));

        Assert.Equal("acme", TenantLookupVariation.FromHost("ACME.myapp.com:443", "myapp.com"));
        Assert.Equal("acme", TenantLookupVariation.FromClaims(user));
        Assert.Equal("acme", TenantLookupVariation.FromPath("/t/acme/projects"));
    }

    [Fact]
    public void Lookup_PositiveNegative_NoTenantPart_GivesNull()
    {
        Assert.Null(TenantLookupVariation.FromHost("myapp.com", "myapp.com"));     // the main site
        Assert.Null(TenantLookupVariation.FromHost("www.myapp.com", "myapp.com"));
        Assert.Null(TenantLookupVariation.FromHost("a.b.myapp.com", "myapp.com")); // nested subdomain
        Assert.Null(TenantLookupVariation.FromPath("/projects"));
    }

    [Fact]
    public void Lookup_NegativeNegative_WrongDomain_OrNotLoggedIn_GivesNull()
    {
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant", "acme")])); // no authenticationType = not logged in

        Assert.Null(TenantLookupVariation.FromHost("acme.evil.com", "myapp.com"));
        Assert.Null(TenantLookupVariation.FromHost("acmemyapp.com", "myapp.com"));
        Assert.Null(TenantLookupVariation.FromClaims(anonymous));
    }
}
