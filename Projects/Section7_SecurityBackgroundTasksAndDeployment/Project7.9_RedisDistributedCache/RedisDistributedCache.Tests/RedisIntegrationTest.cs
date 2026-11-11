using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedisDistributedCache.Models;
using RedisDistributedCache.Tests.Attributes;

namespace RedisDistributedCache.Tests;

// The WHOLE app over HTTP, with a REAL Redis server. Runs only when REDIS_CONNECTION_STRING is set.
// Each test uses its own key prefix (InstanceName), so tests never see each other's data in the shared Redis.
public class RedisIntegrationTest
{
    private readonly string instanceName = "test-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ":";

    private HttpClient CreateClient()
    {
        string connectionString = Environment.GetEnvironmentVariable(RedisFactAttribute.ConnectionStringVariable)!;
        WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Redis", connectionString);
            builder.ConfigureServices(services =>
            {
                services.Configure<RedisCacheOptions>(options => options.InstanceName = instanceName);
            });
        });
        return factory.CreateClient();
    }

    // A separate connection straight to Redis, to look at what the app really stored.
    private IDistributedCache OpenRedisDirectly()
    {
        return new RedisCache(Options.Create(new RedisCacheOptions
        {
            Configuration = Environment.GetEnvironmentVariable(RedisFactAttribute.ConnectionStringVariable),
            InstanceName = instanceName,
        }));
    }

    private static async Task<int> DatabaseReadsAsync(HttpClient client)
    {
        StatsResponse? stats = await client.GetFromJsonAsync<StatsResponse>("/stats");
        return stats!.DatabaseReads;
    }

    [RedisFact]
    public async Task Redis_PositivePositive_SecondRequestIsServedFromRedis()
    {
        HttpClient client = CreateClient();

        ProductPriceModel? first = await client.GetFromJsonAsync<ProductPriceModel>("/prices/2");
        ProductPriceModel? second = await client.GetFromJsonAsync<ProductPriceModel>("/prices/2");

        Assert.Equal(12.50m, first!.Price);
        Assert.Equal(first, second);
        Assert.Equal(1, await DatabaseReadsAsync(client));
        Assert.NotNull(await OpenRedisDirectly().GetStringAsync("price:2")); // really in Redis
    }

    [RedisFact]
    public async Task Redis_PositiveNegative_UnknownProduct_Returns404AndNothingStoredInRedis()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/prices/999");

        Assert.Equal(404, (int)response.StatusCode);
        Assert.Null(await OpenRedisDirectly().GetStringAsync("price:999"));
    }

    [RedisFact]
    public async Task Redis_PositiveNegative_PriceUpdate_DeletesKeyFromRedis()
    {
        HttpClient client = CreateClient();
        await client.GetAsync("/prices/3");
        Assert.NotNull(await OpenRedisDirectly().GetStringAsync("price:3"));

        HttpResponseMessage response = await client.PutAsync("/prices/3?newPrice=199.99", null);

        Assert.Equal(204, (int)response.StatusCode);
        Assert.Null(await OpenRedisDirectly().GetStringAsync("price:3"));
    }

    [RedisFact]
    public async Task Redis_NegativeNegative_InvalidIdOrPrice_Returns400()
    {
        HttpClient client = CreateClient();

        Assert.Equal(400, (int)(await client.GetAsync("/prices/0")).StatusCode);
        Assert.Equal(400, (int)(await client.PutAsync("/prices/1?newPrice=-1", null)).StatusCode);
        Assert.Equal(0, await DatabaseReadsAsync(client));
    }

    private record StatsResponse(int DatabaseReads);
}
