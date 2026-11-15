using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RedisDistributedCache.Databases;
using RedisDistributedCache.Models;
using RedisDistributedCache.Services;
using RedisDistributedCache.Variations;

namespace RedisDistributedCache.Tests;

// The original ProductPriceCacheService vs the variations in RedisDistributedCache/Variations/PriceCacheVariation.cs,
// on the in-memory IDistributedCache (same interface as Redis).
public class VariationsTest
{
    private readonly IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    private readonly ProductPriceDatabase database = new ProductPriceDatabase();
    private readonly ProductPriceCacheService original;
    private readonly PriceCacheVariation variation;

    public VariationsTest()
    {
        original = new ProductPriceCacheService(cache, database);
        variation = new PriceCacheVariation(cache, database);
    }

    [Fact]
    public async Task Read_PositivePositive_BytesVersion_SameProduct_AndCachedAfterFirstRead()
    {
        ProductPriceModel? fromOriginal = await original.GetPriceAsync(1);
        ProductPriceModel? first = await variation.GetWithBytesAsync(1);
        int readsAfterFirst = database.ReadCount;
        ProductPriceModel? second = await variation.GetWithBytesAsync(1);

        Assert.Equal(fromOriginal, first);
        Assert.Equal(first, second);
        Assert.Equal(readsAfterFirst, database.ReadCount); // 2nd read came from the cache
    }

    [Fact]
    public async Task Update_PositiveNegative_InvalidationStrategies_DifferInWhatReadersSee()
    {
        await variation.GetWithBytesAsync(2);                      // cache Mouse at 12.50

        await variation.UpdateWithoutInvalidatingAsync(2, 99m);
        ProductPriceModel? stale = await variation.GetWithBytesAsync(2);

        await variation.UpdateWriteThroughAsync(2, 15m);
        int readsBefore = database.ReadCount;
        ProductPriceModel? fresh = await variation.GetWithBytesAsync(2);

        Assert.Equal(12.50m, stale!.Price);                        // no invalidation: the OLD price until it expires
        Assert.Equal(15m, fresh!.Price);                           // write-through: new price already in the cache...
        Assert.Equal(readsBefore, database.ReadCount);             // ...without another database read
    }

    [Fact]
    public async Task Update_PositiveNegative_OriginalDeletes_SoNextReadGoesToTheDatabase()
    {
        await original.GetPriceAsync(3);
        await original.UpdatePriceAsync(3, 200m);
        int readsBefore = database.ReadCount;

        ProductPriceModel? product = await original.GetPriceAsync(3);

        Assert.Equal(200m, product!.Price);
        Assert.Equal(readsBefore + 1, database.ReadCount);         // delete-on-update: one extra read, never stale
    }

    [Fact]
    public async Task Read_NegativeNegative_UnknownProduct_NothingCachedInEitherVersion()
    {
        Assert.Null(await original.GetPriceAsync(999));
        Assert.Null(await variation.GetWithBytesAsync(999));
        Assert.Null(await cache.GetAsync("bytes:999"));
        Assert.False(await variation.UpdateWriteThroughAsync(999, 1m));
    }
}
