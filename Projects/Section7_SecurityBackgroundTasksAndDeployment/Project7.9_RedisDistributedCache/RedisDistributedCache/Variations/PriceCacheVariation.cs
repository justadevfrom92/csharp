using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RedisDistributedCache.Databases;
using RedisDistributedCache.Models;

namespace RedisDistributedCache.Variations;

// Other ways to write ProductPriceCacheService (original: JSON strings + "delete on update").
public class PriceCacheVariation
{
    private static readonly DistributedCacheEntryOptions fiveMinutes = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
    private readonly IDistributedCache cache;
    private readonly ProductPriceDatabase database;

    public PriceCacheVariation(IDistributedCache cache, ProductPriceDatabase database)
    {
        this.cache = cache;
        this.database = database;
    }

    // BYTES instead of a string: GetAsync/SetAsync with UTF-8 JSON bytes. Same data in Redis, one conversion less
    // (the string methods are just helpers that convert to bytes for you).
    public async Task<ProductPriceModel?> GetWithBytesAsync(int productId)
    {
        byte[]? cached = await cache.GetAsync("bytes:" + productId);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<ProductPriceModel>(cached);
        }
        ProductPriceModel? product = await database.FindAsync(productId);
        if (product != null)
        {
            await cache.SetAsync("bytes:" + productId, JsonSerializer.SerializeToUtf8Bytes(product), fiveMinutes);
        }
        return product;
    }

    // WRITE-THROUGH: on update, PUT the new value in the cache straight away (original DELETES it, so the next
    // reader loads it from the database). Saves one database read; but if two servers update at once, the cache
    // can end up with the OLDER value of the two.
    public async Task<bool> UpdateWriteThroughAsync(int productId, decimal newPrice)
    {
        if (!database.UpdatePrice(productId, newPrice))
        {
            return false;
        }
        ProductPriceModel? product = await database.FindAsync(productId);
        await cache.SetAsync("bytes:" + productId, JsonSerializer.SerializeToUtf8Bytes(product), fiveMinutes);
        return true;
    }

    // NO INVALIDATION: update the database only and wait for the cache entry to expire. Simplest - and readers
    // see the OLD price until then (up to 5 minutes here).
    public Task<bool> UpdateWithoutInvalidatingAsync(int productId, decimal newPrice)
    {
        return Task.FromResult(database.UpdatePrice(productId, newPrice));
    }
}
