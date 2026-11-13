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

**How you can see it:** the `FullPipelineHeaderMiddleware` class (after `UseRouting`) adds an
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

## Custom middleware classes (`Middlewares/`)

Registered in `Program.cs` right after `UseRouting`, in this order:

```csharp
app.UseMiddleware<RequestTimingMiddleware>();      // times every request
app.UseMiddleware<FullPipelineHeaderMiddleware>(); // marks requests that ran the whole pipeline
```

| Class | Style | What it does |
|---|---|---|
| `FullPipelineHeaderMiddleware` | **Convention-based**: constructor takes `RequestDelegate next`, plus a `public Task InvokeAsync(HttpContext)`. No interface. Created **once** at startup. | Adds `X-Full-Pipeline: true` |
| `RequestTimingMiddleware` | **`IMiddleware`**: implements the interface (`InvokeAsync(HttpContext, RequestDelegate next)`). Must be **registered in DI** (`AddTransient<RequestTimingMiddleware>()`); a new one is created **per request**. | Adds `X-Response-Time-Ms` and logs `GET /Catalog/Index -> 200 in 103 ms` |

**How a middleware works:** code before `await next(context)` runs on the way **in**; code after it runs on
the way **out**, once the rest of the pipeline (and the controller) has finished. Headers must be set before
the response body starts, which is why the timing middleware uses `Response.OnStarting(...)`.

**Which style to pick:** convention-based is the common one and slightly faster (one object for the app).
`IMiddleware` is checked by the compiler and, because it's created per request, can take **scoped** services
(like a `DbContext`) in its constructor — a convention-based middleware can't, it would be a captive
dependency (see Project 4.2).

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| robots.txt content | Production → 200, `text/plain`, `Allow: /` | Development → 200 but `Disallow: /` (crawlers told to stay out) | `POST /robots.txt` → 405 Method Not Allowed |
| ShortCircuit | `/robots.txt` has **no** `X-Full-Pipeline` header | `/Catalog/Index` (normal page) **does** have it | `/robots.txt/extra` → 404, went through the full pipeline |
| Request timing middleware | `/Catalog/Uncached` → `X-Response-Time-Ms` ≥ 100 (includes the 100 ms database read) | `/robots.txt` → **no** timing header (short-circuited) | unknown URL → 404, still timed |

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

## Code snippets

**An output cache policy**, from `OutputCachingCatalog/Program.cs`

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Catalog", policy =>
    {
        policy.Expire(TimeSpan.FromSeconds(60)); // never older than 60 s
        policy.Tag("catalog");                   // lets us clear all catalog pages at once
        policy.SetVaryByQuery("page");           // ?page=1 and ?page=2 are cached separately
    });
});

app.UseRouting();
app.UseOutputCache(); // after UseRouting
```

**Cache a page, and clear it when the data changes**, from `Controllers/CatalogController.cs`

```csharp
[OutputCache(PolicyName = "Catalog")]
public async Task<IActionResult> Index()
{
    return View(await database.LoadAllProductsAsync());
}

[HttpPost]
public async Task<IActionResult> ChangePrice(int productId, decimal newPrice)
{
    database.ChangePrice(productId, newPrice);
    await outputCache.EvictByTagAsync("catalog", HttpContext.RequestAborted);
    return RedirectToAction(nameof(Index));
}
```

**A route that skips the rest of the pipeline (`ShortCircuit`)**

```csharp
app.MapControllerRoute(
    name: "robots",
    pattern: "robots.txt",
    defaults: new { controller = "Robots", action = "Get" })
   .ShortCircuit();
```

**A health check**, from `HealthChecks/CatalogDatabaseHealthCheck.cs`

```csharp
public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
{
    List<CatalogProductModel> products = await database.LoadAllProductsAsync();
    if (products.Count == 0)
    {
        return HealthCheckResult.Unhealthy("Catalog is empty.");
    }
    return HealthCheckResult.Healthy($"{products.Count} products");
}

builder.Services.AddHealthChecks().AddCheck<CatalogDatabaseHealthCheck>("catalog-database");
app.MapHealthChecks("/health");
```

**`IMiddleware`-style middleware (created by DI, so it must be registered)**, from `Middlewares/RequestTimingMiddleware.cs`

```csharp
public class RequestTimingMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var timer = Stopwatch.StartNew();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Response-Time-Ms"] = timer.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });
        await next(context);
    }
}

builder.Services.AddTransient<RequestTimingMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
```

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
