using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;

namespace SharedCacheAndSettings.Variations;

// A "cache STAMPEDE": the cache is empty and 20 requests arrive at the same moment. How many of them run the
// slow database query? Open /variations/cache/stampede.
[Route("variations/cache")]
public class CacheStampedeVariation : ControllerBase
{
    private readonly IMemoryCache memoryCache;
    private readonly HybridCache hybridCache;

    public CacheStampedeVariation(IMemoryCache memoryCache, HybridCache hybridCache)
    {
        this.memoryCache = memoryCache;
        this.hybridCache = hybridCache;
    }

    [HttpGet("stampede")]
    public async Task<IActionResult> Stampede()
    {
        string run = Guid.NewGuid().ToString("N"); // a fresh key every time, so the cache always starts empty

        // ORIGINAL style: IMemoryCache.GetOrCreateAsync. NOT protected: every request that finds the key missing
        // runs the factory, so 20 requests at once can mean 20 database queries.
        int memoryCacheLoads = 0;
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => memoryCache.GetOrCreateAsync("memory:" + run, async entry =>
        {
            entry.Size = 1; // this app's cache has a SizeLimit, so every entry needs a size
            Interlocked.Increment(ref memoryCacheLoads);
            await Task.Delay(200);
            return "categories";
        })));

        // TryGetValue + Set by hand: same problem, more code.
        int manualLoads = 0;
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            if (!memoryCache.TryGetValue("manual:" + run, out string? value))
            {
                Interlocked.Increment(ref manualLoads);
                await Task.Delay(200);
                memoryCache.Set("manual:" + run, "categories", new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
            }
        }));

        // HybridCache (.NET 9+): the FIRST caller runs the factory, the other 19 WAIT for its result.
        int hybridLoads = 0;
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => hybridCache.GetOrCreateAsync("hybrid:" + run, async cancellationToken =>
        {
            Interlocked.Increment(ref hybridLoads);
            await Task.Delay(200, cancellationToken);
            return "categories";
        }).AsTask()));

        // Lazy<Task<T>> in a lock-free dictionary: a hand-made way to get the same "only once" behaviour.
        int lazyLoads = 0;
        var once = new System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<Task<string>>>();
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => once.GetOrAdd("lazy", _ => new Lazy<Task<string>>(async () =>
        {
            Interlocked.Increment(ref lazyLoads);
            await Task.Delay(200);
            return "categories";
        })).Value));

        return Ok(new { requests = 20, memoryCacheLoads, manualLoads, hybridLoads, lazyLoads });
    }
}
