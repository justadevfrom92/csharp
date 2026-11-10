using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OutputCachingCatalog.Tests;

// Every behaviour has three kinds of test:
//   PositivePositive - VALID request           -> succeeds
//   PositiveNegative - valid request where a rule says "no" -> correct negative answer
//   NegativeNegative - MALFORMED request (wrong method, wrong URL) -> rejected cleanly
public class RobotsTxtTest
{
    // Starts the real app in memory (no port, no browser) in the given environment.
    private static HttpClient CreateClient(string environmentName)
    {
        WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(environmentName));
        return factory.CreateClient();
    }

    // ---------- robots.txt content ----------

    [Fact]
    public async Task RobotsTxt_PositivePositive_Production_AllowsCrawlersAsPlainText()
    {
        HttpClient client = CreateClient("Production");

        HttpResponseMessage response = await client.GetAsync("/robots.txt");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("User-agent: *", body);
        Assert.Contains("Allow: /\n", body);
    }

    [Fact]
    public async Task RobotsTxt_PositiveNegative_Development_TellsCrawlersToStayOut()
    {
        HttpClient client = CreateClient("Development");

        HttpResponseMessage response = await client.GetAsync("/robots.txt");
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("Disallow: /\n", body);
        Assert.DoesNotContain("Allow: /\n", body.Replace("Disallow: /\n", ""));
    }

    [Fact]
    public async Task RobotsTxt_NegativeNegative_PostInsteadOfGet_Returns405()
    {
        HttpClient client = CreateClient("Production");

        HttpResponseMessage response = await client.PostAsync("/robots.txt", new StringContent(""));

        Assert.Equal(405, (int)response.StatusCode);
    }

    // ---------- ShortCircuit: which requests skip the rest of the pipeline ----------
    // Program.cs adds an "X-Full-Pipeline" header in a middleware AFTER UseRouting.
    // A short-circuited request never reaches that middleware, so it has no such header.

    [Fact]
    public async Task ShortCircuit_PositivePositive_RobotsTxt_SkipsLaterMiddleware()
    {
        HttpClient client = CreateClient("Production");

        HttpResponseMessage response = await client.GetAsync("/robots.txt");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.False(response.Headers.Contains("X-Full-Pipeline"));
    }

    [Fact]
    public async Task ShortCircuit_PositiveNegative_NormalPage_RunsFullPipeline()
    {
        HttpClient client = CreateClient("Production");

        HttpResponseMessage response = await client.GetAsync("/Catalog/Index");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.True(response.Headers.Contains("X-Full-Pipeline"));
    }

    [Fact]
    public async Task ShortCircuit_NegativeNegative_MalformedRobotsUrl_IsNotMatchedAndReturns404()
    {
        HttpClient client = CreateClient("Production");

        HttpResponseMessage response = await client.GetAsync("/robots.txt/extra");

        Assert.Equal(404, (int)response.StatusCode);
        Assert.True(response.Headers.Contains("X-Full-Pipeline")); // no short-circuit route matched
    }
}
