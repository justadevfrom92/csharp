# Project 7.4 — Output Caching Catalog (+ Health Check)

```bash
dotnet run -c Release
```

| URL | What |
|---|---|
| `/Catalog/Index` | Catalog page, **output cached** 60 s, tagged `catalog` |
| `/Catalog/Uncached` | The same page, rendered every time |
| `POST /Catalog/ChangePrice` | Changes a price, then `EvictByTagAsync("catalog")` |
| `/stats` | How many times the (fake, 100 ms) database was read |
| `/health` | Health check with a custom `IHealthCheck` (`Healthy` / `Degraded` / `Unhealthy`) |

## Benchmark

100 requests one after another with `curl`, Release build, on the dev container:

| Page | Total time | Per request | Database reads |
|---|---|---|---|
| `/Catalog/Index` (cached) | ~1.1 s | ~11 ms | **1** |
| `/Catalog/Uncached` | ~11.0 s | ~110 ms | **100** |

So: **~10x faster and 99% fewer database hits**. Almost all of the ~11 ms left is `curl` starting
up — the server answers from memory in well under 1 ms. Under real load the gap gets bigger, because
the uncached page needs a database connection per request and the database becomes the bottleneck.

Repeat it yourself:

```bash
for i in $(seq 100); do curl -s -o /dev/null localhost:5000/Catalog/Index; done; curl localhost:5000/stats
```

## Why these choices

- **Output cache vs `IMemoryCache` (6.3):** `IMemoryCache` saves the *data*; the page still has to be
  rendered. Output caching saves the *final HTML*, so the controller and the view don't run at all.
  Best for pages that look the same for everyone, like a public catalog.
- **Tag-based invalidation:** the policy tags every catalog page with `catalog`. When a price changes,
  one call clears every cached version (`?page=1`, `?page=2`, ...) without knowing their URLs.
- **60-second expiry as well:** a safety net if data changes some other way (another server, a direct
  SQL update) and nobody evicts the tag.
- **`SetVaryByQuery("page")`:** different query values get different cache entries, so page 2 never
  shows page 1's HTML.
- **What NOT to output cache:** anything per user (cart, profile, "Hello Alice"). ASP.NET Core by default
  refuses to cache responses to logged-in users, responses that set cookies, and non-GET requests —
  that's why the form on this page is a plain `<form>` (the tag helper's anti-forgery cookie would
  disable caching).
- **Health check:** load balancers and Kubernetes call `/health` to decide whether to send traffic to
  this server. `Degraded` (slow) is reported separately from `Unhealthy` (broken).

**Demo limit:** `ChangePrice` has no login or anti-forgery check so the demo stays small. A real admin
action would be on a separate, uncached page with `[Authorize(Roles = "Admin")]` and a token (see 7.1 and 7.2).
**Multiple servers:** the default output cache store is in-memory per server. With several servers, use
the Redis output cache store so an eviction clears every server.
