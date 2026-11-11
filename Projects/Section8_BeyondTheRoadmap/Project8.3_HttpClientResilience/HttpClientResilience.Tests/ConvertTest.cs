using System.Net;
using System.Net.Http.Json;
using HttpClientResilience.Clients;
using HttpClientResilience.Models;
using HttpClientResilience.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HttpClientResilience.Tests;

public class ConvertTest
{
    // The real app, but the typed client's network handler is replaced by the scripted fake.
    // Retry delays are made tiny (1 ms) and the attempt timeout 300 ms, so the tests run fast.
    private static HttpClient CreateClient(ScriptedRatesHandlerFake ratesService)
    {
        WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ExchangeRates:BaseAddress", "http://rates.test/");
            builder.UseSetting("ExchangeRates:FirstRetryDelayMilliseconds", "1");
            builder.UseSetting("ExchangeRates:AttemptTimeoutMilliseconds", "300");
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient<ExchangeRateClient>().ConfigurePrimaryHttpMessageHandler(() => ratesService);
            });
        });
        return factory.CreateClient();
    }

    // ---------- Input validation (our own rules, checked before calling anyone) ----------

    [Fact]
    public async Task Convert_PositivePositive_ValidRequest_ReturnsConvertedAmountWithOneCall()
    {
        ScriptedRatesHandlerFake ratesService = new ScriptedRatesHandlerFake().ThenRate(0.92m);
        HttpClient client = CreateClient(ratesService);

        ConversionModel? result = await client.GetFromJsonAsync<ConversionModel>("/convert?from=usd&to=eur&amount=100");

        Assert.Equal(92.00m, result!.ConvertedAmount);
        Assert.Equal(1, ratesService.CallCount);
    }

    [Fact]
    public async Task Convert_PositiveNegative_ZeroAmount_Returns400WithoutCallingRatesService()
    {
        ScriptedRatesHandlerFake ratesService = new();
        HttpClient client = CreateClient(ratesService);

        HttpResponseMessage response = await client.GetAsync("/convert?from=USD&to=EUR&amount=0");

        Assert.Equal(400, (int)response.StatusCode);
        Assert.Equal(0, ratesService.CallCount);
    }

    [Theory]
    [InlineData("/convert?from=US&to=EUR&amount=10")]
    [InlineData("/convert?from=123&to=EUR&amount=10")]
    [InlineData("/convert?to=EUR&amount=10")]
    [InlineData("/convert?from=USD&to=EUR&amount=ten")]
    public async Task Convert_NegativeNegative_MalformedInput_Returns400WithoutCallingRatesService(string url)
    {
        ScriptedRatesHandlerFake ratesService = new();
        HttpClient client = CreateClient(ratesService);

        Assert.Equal(400, (int)(await client.GetAsync(url)).StatusCode);
        Assert.Equal(0, ratesService.CallCount);
    }

    // ---------- Retry ----------

    [Fact]
    public async Task Retry_PositivePositive_TwoFailuresThenSuccess_CallerSeesSuccess()
    {
        ScriptedRatesHandlerFake ratesService = new ScriptedRatesHandlerFake()
            .Then(HttpStatusCode.ServiceUnavailable)
            .Then(HttpStatusCode.InternalServerError)
            .ThenRate(0.92m);
        HttpClient client = CreateClient(ratesService);

        HttpResponseMessage response = await client.GetAsync("/convert?from=USD&to=EUR&amount=10");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal(3, ratesService.CallCount); // 1 try + 2 retries
    }

    [Fact]
    public async Task Retry_PositiveNegative_AlwaysFailing_GivesUpAfter3RetriesAndReturns503()
    {
        ScriptedRatesHandlerFake ratesService = new ScriptedRatesHandlerFake()
            .Then(HttpStatusCode.ServiceUnavailable)
            .Then(HttpStatusCode.ServiceUnavailable)
            .Then(HttpStatusCode.ServiceUnavailable)
            .Then(HttpStatusCode.ServiceUnavailable);
        HttpClient client = CreateClient(ratesService);

        HttpResponseMessage response = await client.GetAsync("/convert?from=USD&to=EUR&amount=10");

        Assert.Equal(503, (int)response.StatusCode);
        Assert.Equal(4, ratesService.CallCount); // 1 try + 3 retries, then stop
    }

    [Fact]
    public async Task Retry_NegativeNegative_ClientErrorFromService_IsNotRetried()
    {
        // 400 means "your request is wrong" - asking again won't help, so it must NOT be retried.
        ScriptedRatesHandlerFake ratesService = new ScriptedRatesHandlerFake().Then(HttpStatusCode.BadRequest);
        HttpClient client = CreateClient(ratesService);

        HttpResponseMessage response = await client.GetAsync("/convert?from=USD&to=EUR&amount=10");

        Assert.Equal(503, (int)response.StatusCode);
        Assert.Equal(1, ratesService.CallCount);
    }

    [Fact]
    public async Task Retry_PositiveNegative_UnknownCurrencyPair404_IsNotRetriedAndReturns404()
    {
        ScriptedRatesHandlerFake ratesService = new ScriptedRatesHandlerFake().Then(HttpStatusCode.NotFound);
        HttpClient client = CreateClient(ratesService);

        HttpResponseMessage response = await client.GetAsync("/convert?from=USD&to=XYZ&amount=10");

        Assert.Equal(404, (int)response.StatusCode);
        Assert.Equal(1, ratesService.CallCount);
    }

    // ---------- Timeout per attempt ----------

    [Fact]
    public async Task Timeout_PositivePositive_FastAnswer_IsUsed()
    {
        ScriptedRatesHandlerFake ratesService = new ScriptedRatesHandlerFake().ThenSlowRate(TimeSpan.FromMilliseconds(50), 0.92m);
        HttpClient client = CreateClient(ratesService);

        Assert.Equal(200, (int)(await client.GetAsync("/convert?from=USD&to=EUR&amount=10")).StatusCode);
        Assert.Equal(1, ratesService.CallCount);
    }

    [Fact]
    public async Task Timeout_PositiveNegative_TooSlowOnce_IsCutOffAndRetried()
    {
        ScriptedRatesHandlerFake ratesService = new ScriptedRatesHandlerFake()
            .ThenSlowRate(TimeSpan.FromSeconds(5), 0.92m)   // longer than the 300 ms limit -> cancelled
            .ThenRate(0.92m);
        HttpClient client = CreateClient(ratesService);
        var timer = System.Diagnostics.Stopwatch.StartNew();

        HttpResponseMessage response = await client.GetAsync("/convert?from=USD&to=EUR&amount=10");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal(2, ratesService.CallCount);
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(3), "the slow attempt must be cut off, not waited for");
    }

    [Fact]
    public async Task Timeout_NegativeNegative_AlwaysTooSlow_Returns503AfterAllAttempts()
    {
        ScriptedRatesHandlerFake ratesService = new ScriptedRatesHandlerFake()
            .ThenSlowRate(TimeSpan.FromSeconds(5), 1m)
            .ThenSlowRate(TimeSpan.FromSeconds(5), 1m)
            .ThenSlowRate(TimeSpan.FromSeconds(5), 1m)
            .ThenSlowRate(TimeSpan.FromSeconds(5), 1m);
        HttpClient client = CreateClient(ratesService);

        HttpResponseMessage response = await client.GetAsync("/convert?from=USD&to=EUR&amount=10");

        Assert.Equal(503, (int)response.StatusCode);
        Assert.Equal(4, ratesService.CallCount);
    }
}
