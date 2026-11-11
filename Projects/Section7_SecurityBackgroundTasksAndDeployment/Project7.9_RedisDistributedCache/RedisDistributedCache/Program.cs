using RedisDistributedCache.Databases;
using RedisDistributedCache.Services;

var builder = WebApplication.CreateBuilder(args);

string? redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    // IDistributedCache backed by Redis: one shared cache for every server running this app.
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "roadmap:"; // prefix for every key, so apps sharing a Redis don't clash
    });
}
else
{
    // No Redis configured: same interface, kept in this process's memory (fine for one server / local dev).
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddSingleton<ProductPriceDatabase>();
builder.Services.AddScoped<ProductPriceCacheService>();
builder.Services.AddControllers();

var app = builder.Build();
app.Logger.LogInformation("Distributed cache: {CacheType}", string.IsNullOrWhiteSpace(redisConnectionString) ? "in-memory" : "Redis at " + redisConnectionString);
app.MapControllers();
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
