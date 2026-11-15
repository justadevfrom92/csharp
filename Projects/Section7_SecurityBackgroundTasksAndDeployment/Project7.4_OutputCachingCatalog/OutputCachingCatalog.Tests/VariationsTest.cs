using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OutputCachingCatalog.Tests;

// Caching attributes compared (OutputCachingCatalog/Variations/CacheAttributeVariation.cs).
// Each response says how many database reads have happened so far: the SAME number twice = the 2nd came from the cache.
public class VariationsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public VariationsTest(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    private async Task<int> ReadsAsync(string path)
    {
        JsonElement body = await client.GetFromJsonAsync<JsonElement>(path);
        return body.GetProperty("databaseReadsSoFar").GetInt32();
    }

    [Theory]
    [InlineData("/variations/cache/policy")]
    [InlineData("/variations/cache/duration")]
    public async Task OutputCache_PositivePositive_SecondCallServedFromCache(string path)
    {
        int first = await ReadsAsync(path);
        int second = await ReadsAsync(path);

        Assert.Equal(first, second); // no new database read
    }

    [Fact]
    public async Task OutputCache_PositiveNegative_QueryStringHandling_DiffersBetweenAttributes()
    {
        int policyPage1 = await ReadsAsync("/variations/cache/policy?page=1");
        int policyPage2 = await ReadsAsync("/variations/cache/policy?page=2");     // the policy varies by "page"
        int ignorePage1 = await ReadsAsync("/variations/cache/ignore-query?page=1");
        int ignorePage2 = await ReadsAsync("/variations/cache/ignore-query?page=2"); // VaryByQueryKeys = []

        Assert.NotEqual(policyPage1, policyPage2); // page 2 was a separate cache entry: the database was read again
        Assert.Equal(ignorePage1, ignorePage2);    // page 2 got page 1's cached copy
    }

    [Fact]
    public async Task ResponseCache_PositiveNegative_OnlySetsAHeader_ServerStillRunsEveryTime()
    {
        HttpResponseMessage response = await client.GetAsync("/variations/cache/response-cache");
        int first = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("databaseReadsSoFar").GetInt32();
        int second = await ReadsAsync("/variations/cache/response-cache");

        Assert.Equal("public, max-age=30", response.Headers.CacheControl!.ToString());
        Assert.True(second > first); // HttpClient (like curl) ignores Cache-Control, so the server ran again
    }

    [Fact]
    public async Task NoCache_NegativeNegative_EveryCallReadsTheDatabase()
    {
        int first = await ReadsAsync("/variations/cache/none");
        int second = await ReadsAsync("/variations/cache/none");

        Assert.True(second > first);
    }
}
