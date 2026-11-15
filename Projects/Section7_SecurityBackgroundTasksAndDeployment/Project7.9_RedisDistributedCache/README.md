# Project 7.9 — Distributed Caching with Redis

| Folder | What |
|---|---|
| `RedisDistributedCache/` | Web API: product prices, cached in Redis (cache-aside) |
| `RedisDistributedCache.Tests/` | Service tests (in-memory cache) + integration tests against a **real** Redis |

## Run

Start Redis:

```bash
redis-server
```

```bash
dotnet run --project RedisDistributedCache
```

```bash
curl localhost:5000/prices/1
```

```bash
curl -X PUT "localhost:5000/prices/1?newPrice=30"
```

```bash
curl localhost:5000/stats
```

The first `GET` takes ~200 ms (database); the second ~1–15 ms (Redis). Measured here: 471 ms → 16 ms.
No Redis? Remove `ConnectionStrings:Redis` from `appsettings.json` and the app uses an in-memory cache instead.

Look inside Redis (each entry is stored as a Redis **hash**, so use `HGETALL`, not `GET`):

```bash
redis-cli HGETALL roadmap:price:1
```

## How it works

- `AddStackExchangeRedisCache(...)` registers `IDistributedCache` backed by Redis. `InstanceName = "roadmap:"`
  prefixes every key, so several apps can share one Redis without clashing.
- `ProductPriceCacheService` uses **cache-aside**:
  1. look in the cache (`GetStringAsync`);
  2. on a miss, read the database;
  3. store the result as JSON (`SetStringAsync`) with an expiry.
- **Expiry:** absolute 5 minutes (data is never older than that) + sliding 1 minute (entries nobody asks for
  disappear sooner, saving memory).
- **Invalidation:** `UpdatePriceAsync` writes to the database, then `RemoveAsync` deletes the cached copy. The next
  read fetches the new price.

## Why Redis instead of `IMemoryCache` (Project 6.3)

`IMemoryCache` lives inside **one** server's memory. With 3 servers behind a load balancer:
- each has its **own** copy — 3× the database reads to fill them;
- updating a price clears the cache on **one** server; the other two keep serving the old price;
- a restart empties the cache.

Redis is **one** cache shared by every server. One `RemoveAsync` fixes it for all of them, and it survives app
restarts. The cost: every cache read is a network call (~1 ms instead of ~0.0001 ms), and Redis is one more
service to run.

**Why code against `IDistributedCache` instead of Redis directly:** the service doesn't know it's Redis. Tests use
the in-memory implementation of the same interface; switching to SQL Server or another cache is a one-line change.

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Get price (service) | 2nd call served from cache, database read once | unknown id 999 → `null`, nothing cached | id 0 / -1 → rejected, database never touched |
| Update price (service) | cached copy removed, next read returns the new price | unknown id → `false`; price 0 → refused, cache kept | id -5 → rejected |
| Whole app with **real Redis** | 2nd HTTP request served from Redis; the key is really in Redis | unknown id → 404 and nothing stored; a PUT deletes the key from Redis | `/prices/0`, `newPrice=-1` → 400, database never touched |

**The real-Redis tests** use `[RedisFact]` (`Attributes/RedisFactAttribute.cs`): they run only when
`REDIS_CONNECTION_STRING` is set, and show as **skipped** otherwise. Each test uses its own random key prefix, so
tests never see each other's data in the shared Redis.

```bash
REDIS_CONNECTION_STRING=localhost:6379 dotnet test RedisDistributedCache.Tests
```

The CI pipeline (7.6) has no Redis server, so these show as **skipped** there; the other tests still run.

## Code snippets

**Use Redis when configured, otherwise an in-memory cache with the same interface**, from `RedisDistributedCache/Program.cs`

```csharp
string? redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "roadmap:"; // prefix for every key
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
```

**Cache-aside: try the cache, else read the database and save to the cache**, from `Services/ProductPriceCacheService.cs`

```csharp
private static readonly DistributedCacheEntryOptions cacheTimes = new()
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    SlidingExpiration = TimeSpan.FromMinutes(1),
};

string? cachedJson = await cache.GetStringAsync(CacheKeyFor(productId));
if (cachedJson != null)
{
    return JsonSerializer.Deserialize<ProductPriceModel>(cachedJson);
}

ProductPriceModel? product = await database.FindAsync(productId);
if (product == null)
{
    return null;
}
await cache.SetStringAsync(CacheKeyFor(productId), JsonSerializer.Serialize(product), cacheTimes);
return product;
```

**Invalidation: delete the cached copy when the data changes**

```csharp
await cache.RemoveAsync(CacheKeyFor(productId)); // one Remove fixes the cache for every server
```

**A test that only runs when Redis is available**, from `RedisDistributedCache.Tests/Attributes/RedisFactAttribute.cs`

```csharp
public class RedisFactAttribute : FactAttribute
{
    public RedisFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")))
        {
            Skip = "Set REDIS_CONNECTION_STRING to run tests against a real Redis server.";
        }
    }
}
```

```bash
REDIS_CONNECTION_STRING=localhost:6379 dotnet test RedisDistributedCache.Tests
```

## Variations

Other ways to write the cache code, in `RedisDistributedCache/Variations/PriceCacheVariation.cs`.
`RedisDistributedCache.Tests/VariationsTest.cs` runs them on the in-memory `IDistributedCache`.

**Storing the value**

| Way | The difference |
|---|---|
| `SetStringAsync(key, JsonSerializer.Serialize(product))` (original) | readable; the string helpers convert to bytes for you |
| `SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(product))` (`GetWithBytesAsync`) | same data in Redis, one conversion less; the test shows identical results |

**When the price changes** (Mouse cached at 12.50)

| Strategy | Next read | Extra database read? | The difference |
|---|---|---|---|
| **delete** the cache entry (original, `cache.RemoveAsync`) | new price | yes, one | never stale; simple |
| **write-through**: put the new value in the cache (`UpdateWriteThroughAsync`) | new price (15.00) | **no** | saves a read; with two servers updating at once the cache can keep the **older** of the two |
| **no invalidation**, wait for expiry (`UpdateWithoutInvalidatingAsync`) | **old price (12.50)** | no | simplest; stale until the entry expires |

**Expiry options** (`DistributedCacheEntryOptions`, documented):

| Setting | Entry is removed |
|---|---|
| `AbsoluteExpirationRelativeToNow = 5 min` | 5 minutes after it was written, however often it's read |
| `SlidingExpiration = 1 min` | after 1 minute **without** being read — a popular entry could live forever on its own |
| both (original) | whichever comes first: popular entries still refresh every 5 minutes |

**Cache technology** (documented): `IMemoryCache` (one server, fastest, 6.3) · `IDistributedCache` + Redis (shared by all
servers, this project) · `HybridCache` (.NET 9+: memory **and** Redis, plus stampede protection — see the 6.3 variations).
