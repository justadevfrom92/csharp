# Project 7.4 — Output Caching Catalog (+ Health Check, robots.txt with ShortCircuit)

> New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

| Folder | What |
|---|---|
| `OutputCachingCatalog/` | The web app |
| `OutputCachingCatalog.Tests/` | xUnit tests for `robots.txt` and `ShortCircuit` (the app runs in memory, no browser) |

```bash
dotnet run -c Release --project OutputCachingCatalog
```

```bash
dotnet test OutputCachingCatalog.Tests
```

| URL | What |
|---|---|
| `/Catalog/Index` | Catalog page, **output cached** 60 s, tagged `catalog` |
| `/Catalog/Uncached` | The same page, rendered every time |
| `POST /Catalog/ChangePrice` | Changes a price, then `EvictByTagAsync("catalog")` |
| `/stats` | How many times the (fake, 100 ms) database was read |
| `/health` | Health check with a custom `IHealthCheck` (`Healthy` / `Degraded` / `Unhealthy`) |
| `/robots.txt` | Rules for search-engine crawlers, from `RobotsController`; the route uses `.ShortCircuit()` |

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

## robots.txt and `ShortCircuit()`

```csharp
app.MapControllerRoute(
    name: "robots",
    pattern: "robots.txt",
    defaults: new { controller = "Robots", action = "Get" })
   .ShortCircuit();
```

- **`pattern: "robots.txt"`** — a fixed URL, no `{controller}`/`{action}` placeholders.
- **`defaults`** — which controller and action handle it: `RobotsController.Get()`
  (`controller = "Robots"`, without the word `Controller`).
- **`.ShortCircuit()`** — normally a request passes through every middleware in `Program.cs`.
  With `ShortCircuit`, as soon as routing matches `/robots.txt` the action runs and the response is
  sent; **every middleware after `UseRouting` is skipped** (here: the `X-Full-Pipeline` header and
  output caching; in bigger apps also authentication and authorization).

**Why an action and not a file in `wwwroot`:** the answer depends on the environment. Production says
`Allow: /`. Every other environment says `Disallow: /`, so a test site never ends up in Google.

**Why short-circuit it:** crawlers request `robots.txt` all the time, it's public, and it never needs a
login, cookies or caching. Skipping the rest of the pipeline saves work on every one of those requests.
Only do this for endpoints like that — a short-circuited endpoint also skips **authorization**, so never
use it on anything that needs a login.

**How you can see it:** `Program.cs` has a tiny middleware after `UseRouting` that adds an
`X-Full-Pipeline: true` header. Check the headers:

```bash
curl -i localhost:5000/robots.txt
```

```bash
curl -i localhost:5000/Catalog/Index
```

The first has no `X-Full-Pipeline` header (short-circuited); the second has it.

**Note:** the default route `{controller}/{action}` also reaches the same action at `/Robots/Get`, and that
URL is *not* short-circuited. Crawlers only ever ask for `/robots.txt`, so it doesn't matter here.

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| robots.txt content | Production → 200, `text/plain`, `Allow: /` | Development → 200 but `Disallow: /` (crawlers told to stay out) | `POST /robots.txt` → 405 Method Not Allowed |
| ShortCircuit | `/robots.txt` has **no** `X-Full-Pipeline` header | `/Catalog/Index` (normal page) **does** have it | `/robots.txt/extra` → 404, went through the full pipeline |

Checked that the tests really test something: with `.ShortCircuit()` removed, the ShortCircuit
PositivePositive test fails.

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

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `record CatalogProductModel` + `with { Price = ... }` | A price change **replaces** the item instead of editing it. A request reading the old item never sees half a change. | A mutable `class` edited in place while another thread reads it. |
| `CatalogDatabase` singleton class | Pretend database shared by all requests. | — |
| `int readCount` field + `Interlocked` | Counted from many requests at once. | See 5.1. |
| `Task<List<CatalogProductModel>>` | Async like a real query; returns a finished copy made inside the lock. | Lazy `IEnumerable` would escape the lock. |
| `class CatalogDatabaseHealthCheck : IHealthCheck` | ASP.NET Core runs every registered `IHealthCheck`; we add ours. | — |
| `IOutputCacheStore` injected | Needed to evict by tag. | — |
| `ViewBag.Cached`, `ViewBag.RenderedAt` | Two tiny extras. | Same `ViewBag` trade-off as 3.1. |
