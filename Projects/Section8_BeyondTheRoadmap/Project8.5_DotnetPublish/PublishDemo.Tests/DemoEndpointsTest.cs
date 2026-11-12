using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PublishDemo.Endpoints;
using PublishDemo.Models;

namespace PublishDemo.Tests;

// PositivePositive - valid request -> succeeds
// PositiveNegative - valid request, but a rule or the situation says "no"
// NegativeNegative - malformed / missing input -> rejected
public class DemoEndpointsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public DemoEndpointsTest(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    // ---------- /greet ----------

    [Fact]
    public async Task Greet_PositivePositive_ValidName_ReturnsGreeting()
    {
        GreetingModel? greeting = await client.GetFromJsonAsync<GreetingModel>("/greet?name=Ada");

        Assert.Equal("Hello, Ada!", greeting!.Message);
    }

    [Fact]
    public async Task Greet_PositiveNegative_NameOverLengthLimit_Returns400()
    {
        string tooLong = new string('a', DemoEndpoints.MaximumNameLength + 1);

        Assert.Equal(400, (int)(await client.GetAsync($"/greet?name={tooLong}")).StatusCode);
        Assert.Equal(200, (int)(await client.GetAsync($"/greet?name={tooLong[..^1]}")).StatusCode); // exactly the limit is fine
    }

    [Theory]
    [InlineData("/greet")]
    [InlineData("/greet?name=")]
    [InlineData("/greet?name=%20%20")]
    public async Task Greet_NegativeNegative_MissingOrBlankName_Returns400(string url)
    {
        Assert.Equal(400, (int)(await client.GetAsync(url)).StatusCode);
    }

    // ---------- /info ----------

    [Fact]
    public async Task Info_PositivePositive_ReportsRuntimeDetails()
    {
        RuntimeInfoModel? info = await client.GetFromJsonAsync<RuntimeInfoModel>("/info");

        Assert.StartsWith(".NET 10", info!.Framework);
        Assert.False(string.IsNullOrWhiteSpace(info.OperatingSystem));
        Assert.False(string.IsNullOrWhiteSpace(info.ExecutablePath));
    }

    [Fact]
    public async Task Info_PositiveNegative_InTests_IsNotNativeAot()
    {
        // Tests run with the normal JIT, so the answer must honestly be "no".
        RuntimeInfoModel? info = await client.GetFromJsonAsync<RuntimeInfoModel>("/info");

        Assert.False(info!.IsNativeAot);
    }

    [Fact]
    public async Task Info_NegativeNegative_WrongMethodOrUnknownPath_IsRejected()
    {
        Assert.Equal(405, (int)(await client.PostAsync("/info", null)).StatusCode);
        Assert.Equal(404, (int)(await client.GetAsync("/information")).StatusCode);
    }
}
