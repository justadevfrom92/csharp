using System.Net;
using System.Net.Http.Json;
using HttpClientResilience.Models;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace HttpClientResilience.Variations;

// Other ways to call the rates service than the TYPED client ExchangeRateClient (original).
public static class RateClientVariation
{
    public const string NamedClient = "rates";

    // NAMED client: registered under a name, created with IHttpClientFactory.CreateClient("rates").
    // Same pooling and resilience as a typed client; the calling code works with a raw HttpClient.
    public static IHttpClientBuilder AddNamedRatesClient(this IServiceCollection services, string baseAddress)
    {
        return services.AddHttpClient(NamedClient, client => client.BaseAddress = new Uri(baseAddress));
    }

    // The STANDARD resilience handler: one line, Microsoft's defaults - retry (3 attempts, exponential, with jitter),
    // a circuit breaker, a 10 s per-attempt timeout and a 30 s total timeout. The original writes its own pipeline
    // (retry + per-attempt timeout) so every number is visible and configurable.
    public static IHttpClientBuilder AddNamedRatesClientWithStandardResilience(this IServiceCollection services, string baseAddress)
    {
        IHttpClientBuilder builder = services.AddNamedRatesClient(baseAddress);
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.Delay = TimeSpan.FromMilliseconds(1); // tiny delays so the demo/tests are fast
            options.Retry.BackoffType = DelayBackoffType.Constant;
        });
        return builder;
    }

    // GetFromJsonAsync: one line - but a 404 THROWS HttpRequestException instead of returning null, so
    // "unknown currency" and "service down" look the same to the caller. The original checks the status first.
    public static Task<ExchangeRateModel?> GetWithGetFromJson(HttpClient client, string from, string to)
    {
        return client.GetFromJsonAsync<ExchangeRateModel>($"rates/{from}/{to}");
    }

    // Status checked first (same logic as ExchangeRateClient): 404 -> null, other errors -> exception.
    public static async Task<ExchangeRateModel?> GetWithStatusCheck(HttpClient client, string from, string to)
    {
        using HttpResponseMessage response = await client.GetAsync($"rates/{from}/{to}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExchangeRateModel>();
    }
}
