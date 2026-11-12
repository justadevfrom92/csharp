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

The CI pipeline (7.6) starts a Redis container so these always run there.
