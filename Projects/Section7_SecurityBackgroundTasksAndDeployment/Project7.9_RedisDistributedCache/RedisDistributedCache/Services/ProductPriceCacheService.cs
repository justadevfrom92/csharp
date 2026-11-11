using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RedisDistributedCache.Databases;
using RedisDistributedCache.Models;

namespace RedisDistributedCache.Services;

// CACHE-ASIDE: look in the cache first; on a miss, read the database and put the result in the cache.
// It only knows IDistributedCache - in production that's Redis, in tests an in-memory version.
public class ProductPriceCacheService
{
    private readonly IDistributedCache cache;
    private readonly ProductPriceDatabase database;

    private static readonly DistributedCacheEntryOptions cacheTimes = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5), // never older than 5 minutes
        SlidingExpiration = TimeSpan.FromMinutes(1),               // dropped early if nobody asks for 1 minute
    };

    public ProductPriceCacheService(IDistributedCache cache, ProductPriceDatabase database)
    {
        this.cache = cache;
        this.database = database;
    }

    public static string CacheKeyFor(int productId)
    {
        return $"price:{productId}";
    }

    public async Task<ProductPriceModel?> GetPriceAsync(int productId)
    {
        CheckProductId(productId);

        // 1. Try the cache. Redis stores bytes/strings, so objects are saved as JSON.
        string? cachedJson = await cache.GetStringAsync(CacheKeyFor(productId));
        if (cachedJson != null)
        {
            return JsonSerializer.Deserialize<ProductPriceModel>(cachedJson);
        }

        // 2. Miss: read the (slow) database.
        ProductPriceModel? product = await database.FindAsync(productId);
        if (product == null)
        {
            return null; // not found: nothing to cache
        }

        // 3. Save for next time.
        await cache.SetStringAsync(CacheKeyFor(productId), JsonSerializer.Serialize(product), cacheTimes);
        return product;
    }

    public async Task<bool> UpdatePriceAsync(int productId, decimal newPrice)
    {
        CheckProductId(productId);
        if (newPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrice), "Price must be more than 0.");
        }

        if (!database.UpdatePrice(productId, newPrice))
        {
            return false;
        }

        // INVALIDATION: delete the cached copy, so the next read gets the new price from the database.
        // Every server shares the same Redis, so this one Remove fixes the cache for all of them.
        await cache.RemoveAsync(CacheKeyFor(productId));
        return true;
    }

    private static void CheckProductId(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId), "Product id must be more than 0.");
        }
    }
}
