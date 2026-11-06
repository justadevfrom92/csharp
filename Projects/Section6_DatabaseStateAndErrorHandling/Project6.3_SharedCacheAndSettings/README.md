# Project 6.3 — Global Cache & App Settings

```bash
dotnet run
```

| URL | What it shows |
|---|---|
| `GET /` | Settings loaded from `appsettings.json` via `IOptions<ShopSettings>` |
| `GET /categories` | Categories — first call ~500 ms (database), then ~0 ms (cache) |
| `POST /categories/clear-cache` | Removes the cached categories |

```bash
curl localhost:5000/categories
```

```bash
curl -X POST localhost:5000/categories/clear-cache
```

## What it covers

- `IOptions<ShopSettings>` bound from config, checked with `[Range]`/`[Required]`, `ValidateOnStart()`.
- `IMemoryCache` with `GetOrCreateAsync`, expiry time and a size limit.
- `AddSingleton` for shared, app-wide state.

## Why these choices

**Settings with `IOptions<T>` instead of reading config everywhere.** Settings are read and checked
once at startup. A typo like `CategoryCacheMinutes: 0` stops the app *immediately* with a clear
message instead of failing later in some request. Code gets a typed object (`settings.ShopName`)
instead of magic strings (`config["Shop:ShopName"]`).

**Singleton = "global variable" done safely.** Instead of a `static` field, the DI container holds
one `CategoryService` for the whole app. It's still easy to replace in tests, and it only holds
things that are safe to share between threads (`IMemoryCache` is thread-safe; the counter uses `Interlocked`).
**Never** put per-user or per-request data in a singleton — every user would see it.

## Cache invalidation trade-offs

| Approach | Used here? | Pro | Con |
|---|---|---|---|
| Time-based expiry (`CategoryCacheMinutes`) | Yes | Simple, data is never older than N minutes | Users can see old data for up to N minutes |
| Remove on change (`ClearCachedCategories`) | Yes | Fresh data right after a change | Every place that changes data must remember to call it |
| No cache | — | Always fresh | Every request hits the database (500 ms each here) |

We use **both**: clear the cache when we know data changed, and expiry as a safety net in case
something changed without telling us (e.g. someone edited the database directly).

**How long to cache?** Categories almost never change, so 5 minutes is safe. Prices or stock
should be cached for seconds, or not at all.

## Memory footprint — avoiding leaks

- A cache without limits is a memory leak with a nicer name: if keys include user input
  (e.g. `search:{text}`) it grows forever. `SizeLimit` caps the number of items; when full, new
  entries are simply not added (and expired ones get removed).
- Every entry has an expiry, so nothing stays forever.
- Only small, shared data is cached (a list of category names). Big or per-user data doesn't belong here.
- `IMemoryCache` lives in *one* server's memory. With several servers each has its own copy and
  clearing it on one doesn't clear the others — that's when you move to Redis (`IDistributedCache`).

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `class ShopSettings` with setters | The options binder creates it empty and sets each value from JSON. `[Range]`/`[Required]` check them at startup. | A record with a constructor can't be bound this way. |
| `IOptions<ShopSettings>` (not `IOptionsMonitor`) | Settings are read once at startup and don't change while running. | `IOptionsMonitor` is for live-reload (used for feature flags in 7.5). |
| `settings.Value` stored in a field | Read once in the constructor, then just a normal object. | — |
| `CategoryService` as **singleton class** | One shared service holding the cache and settings — both thread-safe. | Scoped would be fine too, but the cache is the shared part anyway. |
| `int databaseCallCount` field + `Interlocked` | Several requests can hit it at once. | `count++` would lose updates (see 5.1). |
| `const string CategoriesCacheKey` | One spelling for the key, used in get and remove. | Typing the string twice risks a typo that silently never clears. |
| `List<string>` categories | A finished list (cache must store real data, not a lazy query). | Caching a lazy `IEnumerable` would re-run the "database" call on every loop. |
| Anonymous object `new { categories, milliseconds, ... }` | Only used to shape one JSON response. | A named record would be extra code used once. |
| `ControllerBase` + `[ApiController]` | JSON only, no views. | `Controller` adds view support we don't use. |
