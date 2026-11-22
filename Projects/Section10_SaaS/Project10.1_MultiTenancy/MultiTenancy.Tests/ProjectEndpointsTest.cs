using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MultiTenancy.Models;

namespace MultiTenancy.Tests;

// Through HTTP: the middleware reads X-Tenant, and nothing leaks between tenants.
public class ProjectEndpointsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ProjectEndpointsTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private HttpClient ClientFor(string? tenantId)
    {
        HttpClient client = factory.CreateClient();
        if (tenantId != null)
        {
            client.DefaultRequestHeaders.Add("X-Tenant", tenantId);
        }
        return client;
    }

    [Fact]
    public async Task Projects_PositivePositive_EachTenantGetsItsOwnList()
    {
        List<ProjectModel>? acme = await ClientFor("acme").GetFromJsonAsync<List<ProjectModel>>("/projects");
        List<ProjectModel>? globex = await ClientFor("globex").GetFromJsonAsync<List<ProjectModel>>("/projects");
        TenantModel? me = await ClientFor("globex").GetFromJsonAsync<TenantModel>("/me");

        Assert.All(acme!, project => Assert.Equal("acme", project.TenantId));
        Assert.Contains(acme!, project => project.Name == "Website");
        Assert.All(globex!, project => Assert.Equal("globex", project.TenantId));
        Assert.Equal("Globex Corp", me!.Name);
    }

    [Fact]
    public async Task Projects_PositiveNegative_TenantIdInTheBodyIsIgnored_HeaderCaseDoesNotMatter()
    {
        HttpResponseMessage response = await ClientFor("ACME").PostAsJsonAsync("/projects", new { name = "Launch", tenantId = "globex" });
        ProjectModel? created = await response.Content.ReadFromJsonAsync<ProjectModel>();
        List<ProjectModel>? globex = await ClientFor("globex").GetFromJsonAsync<List<ProjectModel>>("/projects");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("acme", created!.TenantId);
        Assert.DoesNotContain(globex!, project => project.Name == "Launch");
    }

    [Fact]
    public async Task Projects_NegativeNegative_NoHeader400_UnknownTenant404_OtherTenantsProject404_BadName400()
    {
        HttpClient acme = ClientFor("acme");

        Assert.Equal(HttpStatusCode.BadRequest, (await ClientFor(null).GetAsync("/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ClientFor("initech").GetAsync("/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await acme.GetAsync("/projects/3")).StatusCode);    // globex's
        Assert.Equal(HttpStatusCode.NotFound, (await acme.PutAsJsonAsync("/projects/3", new { name = "Hacked" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await acme.DeleteAsync("/projects/3")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await acme.PostAsJsonAsync("/projects", new { name = "" })).StatusCode);
        Assert.Equal("Rocket", (await ClientFor("globex").GetFromJsonAsync<ProjectModel>("/projects/3"))!.Name); // untouched
    }
}
