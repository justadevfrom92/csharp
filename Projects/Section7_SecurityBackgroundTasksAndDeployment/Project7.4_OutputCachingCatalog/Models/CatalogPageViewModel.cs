using OutputCachingCatalog.Data.Entities;

namespace OutputCachingCatalog.Models;

// What the catalog page shows. The same view is used for the cached and the uncached page.
public class CatalogPageViewModel
{
    public List<CatalogProduct> Products { get; set; } = new();

    // When the HTML was built. On the cached page this stays the same until the cache is cleared.
    public string RenderedAt { get; set; } = "";

    public bool IsOutputCached { get; set; }
}
