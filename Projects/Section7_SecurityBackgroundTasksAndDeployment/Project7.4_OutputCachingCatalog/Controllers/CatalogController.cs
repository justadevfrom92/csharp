using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using OutputCachingCatalog.Models;

namespace OutputCachingCatalog.Controllers;

public class CatalogController : Controller
{
    private readonly CatalogDatabase database;
    private readonly IOutputCacheStore outputCache;

    public CatalogController(CatalogDatabase database, IOutputCacheStore outputCache)
    {
        this.database = database;
        this.outputCache = outputCache;
    }

    // The whole rendered HTML page is stored on the server for 60 seconds (policy "Catalog").
    [OutputCache(PolicyName = "Catalog")]
    public async Task<IActionResult> Index()
    {
        ViewBag.RenderedAt = DateTime.Now.ToString("HH:mm:ss.fff");
        ViewBag.Cached = true;
        return View(await database.LoadAllProductsAsync());
    }

    // Same page, never cached - for comparison.
    public async Task<IActionResult> Uncached()
    {
        ViewBag.RenderedAt = DateTime.Now.ToString("HH:mm:ss.fff");
        ViewBag.Cached = false;
        return View("Index", await database.LoadAllProductsAsync());
    }

    // Changing data throws away every cached page tagged "catalog", so nobody sees the old price.
    [HttpPost]
    public async Task<IActionResult> ChangePrice(int productId, decimal newPrice)
    {
        database.ChangePrice(productId, newPrice);
        await outputCache.EvictByTagAsync("catalog", HttpContext.RequestAborted);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/stats")]
    public IActionResult Stats()
    {
        return Json(new { databaseReads = database.ReadCount });
    }
}
