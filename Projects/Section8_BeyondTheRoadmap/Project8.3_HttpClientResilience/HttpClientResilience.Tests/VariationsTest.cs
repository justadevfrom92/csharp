using System.Net;
using HttpClientResilience.Models;
using HttpClientResilience.Tests.Fakes;
using HttpClientResilience.Variations;
using Microsoft.Extensions.DependencyInjection;

namespace HttpClientResilience.Tests;

// Named clients, the standard resilience handler, and two ways of reading the answer
// (HttpClientResilience/Variations/RateClientVariation.cs). The network is the scripted fake, which counts calls.
public class VariationsTest
{
    private static HttpClient NamedClient(ScriptedRatesHandlerFake fake, bool withStandardResilience)
    {
        var services = new ServiceCollection();
        IHttpClientBuilder builder = withStandardResilience
            ? services.AddNamedRatesClientWithStandardResilience("http://rates.test/")
            : services.AddNamedRatesClient("http://rates.test/");
        builder.ConfigurePrimaryHttpMessageHandler(() => fake);
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient(RateClientVariation.NamedClient);
    }

    [Fact]
    public async Task Read_PositivePositive_HealthyService_BothReadingStylesGiveTheRate()
    {
        var fake = new ScriptedRatesHandlerFake().ThenRate(0.92m).ThenRate(0.92m);
        HttpClient client = NamedClient(fake, withStandardResilience: false);

        ExchangeRateModel? viaGetFromJson = await RateClientVariation.GetWithGetFromJson(client, "USD", "EUR");
        ExchangeRateModel? viaStatusCheck = await RateClientVariation.GetWithStatusCheck(client, "USD", "EUR");

        Assert.Equal(0.92m, viaGetFromJson!.Rate);
        Assert.Equal(viaGetFromJson, viaStatusCheck);
    }

    [Fact]
    public async Task Read_PositiveNegative_UnknownCurrency404_GetFromJsonThrows_StatusCheckReturnsNull()
    {
        var fake = new ScriptedRatesHandlerFake().Then(HttpStatusCode.NotFound).Then(HttpStatusCode.NotFound);
        HttpClient client = NamedClient(fake, withStandardResilience: false);

        await Assert.ThrowsAsync<HttpRequestException>(() => RateClientVariation.GetWithGetFromJson(client, "USD", "XXX"));
        Assert.Null(await RateClientVariation.GetWithStatusCheck(client, "USD", "XXX"));
    }

    [Fact]
    public async Task Resilience_PositiveNegative_TwoFailuresThenSuccess_OnlyTheResilientClientRecovers()
    {
        var plainFake = new ScriptedRatesHandlerFake().Then(HttpStatusCode.ServiceUnavailable);
        var resilientFake = new ScriptedRatesHandlerFake().Then(HttpStatusCode.ServiceUnavailable).Then(HttpStatusCode.ServiceUnavailable).ThenRate(0.92m);

        await Assert.ThrowsAsync<HttpRequestException>(() => RateClientVariation.GetWithStatusCheck(NamedClient(plainFake, false), "USD", "EUR"));
        ExchangeRateModel? rate = await RateClientVariation.GetWithStatusCheck(NamedClient(resilientFake, true), "USD", "EUR");

        Assert.Equal(1, plainFake.CallCount);      // no resilience: one try, then failure
        Assert.Equal(0.92m, rate!.Rate);
        Assert.Equal(3, resilientFake.CallCount);  // standard handler retried twice
    }

    [Fact]
    public async Task Resilience_NegativeNegative_404IsNotRetried_EvenByTheStandardHandler()
    {
        var fake = new ScriptedRatesHandlerFake().Then(HttpStatusCode.NotFound);

        Assert.Null(await RateClientVariation.GetWithStatusCheck(NamedClient(fake, true), "USD", "XXX"));
        Assert.Equal(1, fake.CallCount); // asking again won't change a 404
    }
}
