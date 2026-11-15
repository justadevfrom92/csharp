using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ServiceRegistration.Notifiers;
using ServiceRegistration.Services;

namespace ServiceRegistration.Tests;

public class RegistrationsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public RegistrationsTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private class FixedPriceService : IPriceService
    {
        public decimal GetPrice(int productId)
        {
            return 1m;
        }
    }

    private class NeedsMissingDependencyService
    {
        public NeedsMissingDependencyService(IDisposable notRegistered)
        {
        }
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string path)
    {
        return await client.GetFromJsonAsync<JsonElement>(path);
    }

    // ---------- lifetimes, factory, open generic, many implementations, decorator ----------

    [Fact]
    public async Task Registrations_PositivePositive_EachKindResolvesAsExpected()
    {
        HttpClient client = factory.CreateClient();

        JsonElement first = await GetJsonAsync(client, "/lifetimes");
        JsonElement second = await GetJsonAsync(client, "/lifetimes");
        JsonElement repositories = await GetJsonAsync(client, "/repositories");
        JsonElement notifiers = await GetJsonAsync(client, "/notifiers");
        JsonElement price = await GetJsonAsync(client, "/prices/5");

        Assert.True(first.GetProperty("singletonSameTwice").GetBoolean());
        Assert.True(first.GetProperty("scopedSameTwice").GetBoolean());
        Assert.False(first.GetProperty("transientSameTwice").GetBoolean());
        Assert.Equal(first.GetProperty("singletonId").GetGuid(), second.GetProperty("singletonId").GetGuid());   // across requests: same
        Assert.NotEqual(first.GetProperty("scopedId").GetGuid(), second.GetProperty("scopedId").GetGuid());      // new request = new scope
        Assert.Equal("Welcome to Roadmap Store, Ann!", await client.GetFromJsonAsync<string>("/greeting/Ann"));
        Assert.Equal(1, repositories.GetProperty("customers").GetInt32());
        Assert.Equal(["email", "sms"], notifiers.GetProperty("all").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("sms", notifiers.GetProperty("whenAskingForOne").GetString()); // the LAST registration wins
        Assert.Equal("CachingPriceServiceDecorator", price.GetProperty("decoratedBy").GetString());
    }

    [Fact]
    public async Task Registrations_PositiveNegative_TestsCanReplaceAndRemoveRegistrations()
    {
        var onlyEmail = new EmailNotifier();
        HttpClient client = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IPriceService, FixedPriceService>()); // swap one
            services.RemoveAll<INotifier>();                                                   // remove all...
            services.AddSingleton<INotifier>(onlyEmail);                                       // ...and add an INSTANCE
        })).CreateClient();

        JsonElement price = await GetJsonAsync(client, "/prices/5");
        JsonElement notifiers = await GetJsonAsync(client, "/notifiers");

        Assert.Equal(1m, price.GetProperty("price").GetDecimal());
        Assert.Equal("FixedPriceService", price.GetProperty("decoratedBy").GetString());
        Assert.Equal(["email"], notifiers.GetProperty("all").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/prices/0")).StatusCode);
    }

    [Theory]
    [InlineData("Shop:ShopName", "")]
    [InlineData("Shop:SupportEmail", "not-an-email")]
    [InlineData("JobRunner:TickMilliseconds", "1")]
    public void Startup_NegativeNegative_InvalidSettings_AppRefusesToStart(string key, string value)
    {
        using WebApplicationFactory<Program> broken = factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));

        Assert.Throws<OptionsValidationException>(() => broken.CreateClient()); // ValidateOnStart
    }

    [Fact]
    public void Startup_NegativeNegative_ServiceWithMissingDependency_CaughtAtStartupByValidateOnBuild()
    {
        using WebApplicationFactory<Program> broken = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<NeedsMissingDependencyService>()));

        var error = Assert.Throws<AggregateException>(() => broken.CreateClient());

        Assert.Contains("IDisposable", error.Message);
    }
}
