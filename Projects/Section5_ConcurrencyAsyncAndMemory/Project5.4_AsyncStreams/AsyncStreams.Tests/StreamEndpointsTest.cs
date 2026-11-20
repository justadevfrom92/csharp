using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AsyncStreams.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AsyncStreams.Tests;

public class StreamEndpointsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public StreamEndpointsTest(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Ticks_PositivePositive_FirstTickArrivesBeforeTheLastIsMade()
    {
        var timer = Stopwatch.StartNew();
        var arrivals = new List<long>();

        await foreach (PriceTickModel? _ in client.GetFromJsonAsAsyncEnumerable<PriceTickModel>("/ticks?count=3&delayMs=800"))
        {
            arrivals.Add(timer.ElapsedMilliseconds);
        }

        Assert.Equal(3, arrivals.Count);
        Assert.True(arrivals[0] < 2000, $"first tick at {arrivals[0]} ms");  // ~800 ms, not ~2400
        Assert.True(arrivals[2] >= 2300, $"last tick at {arrivals[2]} ms");
    }

    [Fact]
    public async Task Ticks_PositiveNegative_BufferedVersion_SameTicks_ButNothingUntilTheEnd()
    {
        string streamed = await client.GetStringAsync("/ticks?count=3&delayMs=10");
        var timer = Stopwatch.StartNew();
        HttpResponseMessage buffered = await client.GetAsync("/variations/ticks/buffered?count=3&delayMs=800", HttpCompletionOption.ResponseHeadersRead);
        long headersAt = timer.ElapsedMilliseconds;

        Assert.True(headersAt >= 2300, $"buffered headers at {headersAt} ms"); // even the status waits for every tick
        Assert.Equal(streamed, await buffered.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ticks_NegativeNegative_StreamFailsHalfway_Client_Gets200_ThenTheBodyIsCutOff()
    {
        HttpResponseMessage response = await client.GetAsync("/variations/ticks/broken", HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // status was sent before the error - no 500 possible
        HttpRequestException cutOff = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync());
        Assert.Equal("Price feed went down.", cutOff.InnerException!.InnerException!.Message);
    }

    [Fact]
    public async Task Orders_PositivePositive_RowsStreamedAsJsonArray()
    {
        List<OrderModel>? orders = await client.GetFromJsonAsync<List<OrderModel>>("/orders/big?min=100");

        Assert.Equal([2, 3, 5], orders!.Select(order => order.Id));
        Assert.Equal(1419m, await client.GetFromJsonAsync<decimal>("/orders/big/sum?min=100"));
    }

    [Fact]
    public async Task Orders_PositiveNegative_HighMinimum_EmptyArray()
    {
        Assert.Equal("[]", await client.GetStringAsync("/orders/big?min=100000"));
    }

    [Fact]
    public async Task Orders_NegativeNegative_NotANumber_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/orders/big?min=lots")).StatusCode);
    }
}
