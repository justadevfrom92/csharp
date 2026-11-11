using Microsoft.AspNetCore.Mvc.Testing;

namespace OutputCachingCatalog.Tests;

// Every behaviour has three kinds of test:
//   PositivePositive - VALID request           -> succeeds
//   PositiveNegative - valid request where a rule says "no" -> correct negative answer
//   NegativeNegative - MALFORMED request (unknown URL)      -> rejected cleanly
public class RequestTimingMiddlewareTest
{
    private static HttpClient CreateClient()
    {
        return new WebApplicationFactory<Program>().CreateClient();
    }

    private static long ReadResponseTime(HttpResponseMessage response)
    {
        string headerValue = response.Headers.GetValues("X-Response-Time-Ms").Single();
        return long.Parse(headerValue);
    }

    [Fact]
    public async Task RequestTiming_PositivePositive_NormalPage_HasRealResponseTime()
    {
        HttpClient client = CreateClient();

        // The uncached page reads the fake database, which always takes 100 ms.
        HttpResponseMessage response = await client.GetAsync("/Catalog/Uncached");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.True(ReadResponseTime(response) >= 100, "the time should include the 100 ms database read");
    }

    [Fact]
    public async Task RequestTiming_PositiveNegative_ShortCircuitedRobotsTxt_IsNotTimed()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/robots.txt");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.False(response.Headers.Contains("X-Response-Time-Ms")); // ShortCircuit skipped the middleware
    }

    [Fact]
    public async Task RequestTiming_NegativeNegative_UnknownUrl_Returns404AndIsStillTimed()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/this/page/does/not/exist");

        Assert.Equal(404, (int)response.StatusCode);
        Assert.True(ReadResponseTime(response) >= 0);
    }
}
