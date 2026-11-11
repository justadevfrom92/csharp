using Microsoft.AspNetCore.Mvc.Testing;
using RateLimiting.Constants;

namespace RateLimiting.Tests;

// Every test starts a NEW app, so every limiter starts with a full quota.
public class RateLimitingTest
{
    private static HttpClient CreateClient()
    {
        return new WebApplicationFactory<Program>().CreateClient();
    }

    private static Task<HttpResponseMessage> GetQuotesAsync(HttpClient client, string? clientId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/quotes");
        if (clientId != null)
        {
            request.Headers.Add(RateLimitPolicyConstants.ClientIdHeader, clientId);
        }
        return client.SendAsync(request);
    }

    // ---------- Fixed window, per client ----------

    [Fact]
    public async Task FixedWindow_PositivePositive_UpToTheLimit_AllAllowed()
    {
        HttpClient client = CreateClient();

        for (int requestNumber = 1; requestNumber <= RateLimitPolicyConstants.RequestsPerWindow; requestNumber++)
        {
            Assert.Equal(200, (int)(await GetQuotesAsync(client, "alice")).StatusCode);
        }
    }

    [Fact]
    public async Task FixedWindow_PositiveNegative_OneOverTheLimit_Returns429WithRetryAfter_OtherClientsUnaffected()
    {
        HttpClient client = CreateClient();
        for (int requestNumber = 1; requestNumber <= RateLimitPolicyConstants.RequestsPerWindow; requestNumber++)
        {
            await GetQuotesAsync(client, "alice");
        }

        HttpResponseMessage rejected = await GetQuotesAsync(client, "alice");

        Assert.Equal(429, (int)rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter?.Delta);
        Assert.True(rejected.Headers.RetryAfter!.Delta!.Value.TotalSeconds <= RateLimitPolicyConstants.WindowSeconds);
        Assert.Equal(200, (int)(await GetQuotesAsync(client, "bob")).StatusCode);  // bob has his own quota
        Assert.Equal(200, (int)(await client.GetAsync("/health")).StatusCode);    // health is never limited
    }

    [Fact]
    public async Task FixedWindow_NegativeNegative_MissingClientId_Returns400AndUsesNoQuota()
    {
        HttpClient client = CreateClient();

        for (int requestNumber = 1; requestNumber <= 10; requestNumber++)
        {
            Assert.Equal(400, (int)(await GetQuotesAsync(client, null)).StatusCode); // never 429
        }
    }

    // ---------- Concurrency: one report at a time ----------

    [Fact]
    public async Task Concurrency_PositivePositive_ReportsOneAfterAnother_AllAllowed()
    {
        HttpClient client = CreateClient();

        Assert.Equal(200, (int)(await client.GetAsync("/api/report?seconds=0.05")).StatusCode);
        Assert.Equal(200, (int)(await client.GetAsync("/api/report?seconds=0.05")).StatusCode);
    }

    [Fact]
    public async Task Concurrency_PositiveNegative_SecondReportWhileFirstRuns_Returns429()
    {
        HttpClient client = CreateClient();

        Task<HttpResponseMessage> slowReport = client.GetAsync("/api/report?seconds=1");
        await Task.Delay(200); // let the first one take the only slot
        HttpResponseMessage secondReport = await client.GetAsync("/api/report?seconds=0.05");

        Assert.Equal(429, (int)secondReport.StatusCode);
        Assert.Equal(200, (int)(await slowReport).StatusCode);
    }

    [Fact]
    public async Task Concurrency_NegativeNegative_MalformedRequest_Returns400AndReleasesTheSlot()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage malformed = await client.GetAsync("/api/report?seconds=abc");
        HttpResponseMessage next = await client.GetAsync("/api/report?seconds=0.05");

        Assert.Equal(400, (int)malformed.StatusCode);
        Assert.Equal(200, (int)next.StatusCode); // the failed request didn't keep the slot
    }
}
