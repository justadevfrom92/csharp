using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OutputCachingCatalog.Databases;

namespace OutputCachingCatalog.Variations;

// The same "load the catalog" action with different caching attributes. Each returns how many times the
// database has been read SO FAR (one counter shared by the whole app), so calling it twice shows whether the
// second call was served from the cache. Try /variations/cache/....
[Route("variations/cache")]
public class CacheAttributeVariation : Controller
{
    private readonly CatalogDatabase database;

    public CacheAttributeVariation(CatalogDatabase database)
    {
        this.database = database;
    }

    // ORIGINAL style: a named policy from Program.cs (60 s, tag "catalog", varies by ?page).
    [HttpGet("policy")]
    [OutputCache(PolicyName = "Catalog")]
    public Task<IActionResult> WithPolicy()
    {
        return LoadAsync();
    }

    // Settings written on the attribute itself: quick, but repeated on every action that needs them.
    [HttpGet("duration")]
    [OutputCache(Duration = 30)]
    public Task<IActionResult> WithDuration()
    {
        return LoadAsync();
    }

    // VaryByQueryKeys = [] means "ignore the query string": ?page=1 and ?page=2 get the SAME cached copy.
    [HttpGet("ignore-query")]
    [OutputCache(Duration = 30, VaryByQueryKeys = [])]
    public Task<IActionResult> IgnoringQuery(int page)
    {
        return LoadAsync();
    }

    // [ResponseCache]: DIFFERENT feature with a similar name. It only writes a Cache-Control HEADER telling the
    // BROWSER (and proxies) to keep a copy. The server still runs the action every time someone asks it.
    [HttpGet("response-cache")]
    [ResponseCache(Duration = 30)]
    public Task<IActionResult> WithResponseCache()
    {
        return LoadAsync();
    }

    // No caching: the database is read every time.
    [HttpGet("none")]
    public Task<IActionResult> WithoutCache()
    {
        return LoadAsync();
    }

    private async Task<IActionResult> LoadAsync()
    {
        await database.LoadAllProductsAsync();
        return Json(new { databaseReadsSoFar = database.ReadCount, page = Request.Query["page"].ToString() });
    }
}
