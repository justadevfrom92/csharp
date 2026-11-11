using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RedisDistributedCache.Databases;
using RedisDistributedCache.Models;
using RedisDistributedCache.Services;

namespace RedisDistributedCache.Tests;

// The service only knows IDistributedCache, so these tests use the IN-MEMORY implementation of that
// interface: fast, isolated (a new cache per test), and no Redis needed.
// PositivePositive - valid input -> succeeds
// PositiveNegative - well-formed input that a rule refuses (unknown product, price 0) -> nothing cached/changed
// NegativeNegative - malformed input (id 0 or negative) -> rejected
public class ProductPriceCacheServiceTest
{
    private readonly ProductPriceDatabase database = new();
    private readonly IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    private readonly ProductPriceCacheService service;

    public ProductPriceCacheServiceTest()
    {
        service = new ProductPriceCacheService(cache, database);
    }

    // ---------- GetPrice ----------

    [Fact]
    public async Task GetPrice_PositivePositive_KnownProduct_SecondCallComesFromCache()
    {
        ProductPriceModel? first = await service.GetPriceAsync(1);
        ProductPriceModel? second = await service.GetPriceAsync(1);

        Assert.Equal(25.00m, first!.Price);
        Assert.Equal(first, second);
        Assert.Equal(1, database.ReadCount); // the second call never touched the database
        Assert.NotNull(await cache.GetStringAsync(ProductPriceCacheService.CacheKeyFor(1)));
    }

    [Fact]
    public async Task GetPrice_PositiveNegative_UnknownProduct_ReturnsNullAndCachesNothing()
    {
        ProductPriceModel? result = await service.GetPriceAsync(999);

        Assert.Null(result);
        Assert.Null(await cache.GetStringAsync(ProductPriceCacheService.CacheKeyFor(999)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPrice_NegativeNegative_InvalidId_IsRejectedWithoutTouchingDatabase(int productId)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetPriceAsync(productId));
        Assert.Equal(0, database.ReadCount);
    }

    // ---------- UpdatePrice (cache invalidation) ----------

    [Fact]
    public async Task UpdatePrice_PositivePositive_RemovesCachedCopySoNextReadIsFresh()
    {
        await service.GetPriceAsync(1); // now cached at 25.00

        bool updated = await service.UpdatePriceAsync(1, 30.00m);
        ProductPriceModel? afterUpdate = await service.GetPriceAsync(1);

        Assert.True(updated);
        Assert.Equal(30.00m, afterUpdate!.Price); // not the stale 25.00 from the cache
        Assert.Equal(2, database.ReadCount);
    }

    [Fact]
    public async Task UpdatePrice_PositiveNegative_UnknownProduct_ReturnsFalse()
    {
        Assert.False(await service.UpdatePriceAsync(999, 10m));
    }

    [Fact]
    public async Task UpdatePrice_PositiveNegative_ZeroPrice_IsRefusedAndCacheKept()
    {
        await service.GetPriceAsync(1);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.UpdatePriceAsync(1, 0m));
        Assert.NotNull(await cache.GetStringAsync(ProductPriceCacheService.CacheKeyFor(1)));
        Assert.Equal(25.00m, (await service.GetPriceAsync(1))!.Price);
    }

    [Fact]
    public async Task UpdatePrice_NegativeNegative_InvalidId_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.UpdatePriceAsync(-5, 10m));
    }
}
