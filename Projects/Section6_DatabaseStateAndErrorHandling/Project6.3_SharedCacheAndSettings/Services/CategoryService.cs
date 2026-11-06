using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SharedCacheAndSettings.Settings;

namespace SharedCacheAndSettings.Services;

// Reads categories from a (fake, slow) database, keeping a copy in memory.
public class CategoryService
{
    private const string CategoriesCacheKey = "categories:all";
    private readonly IMemoryCache cache;
    private readonly ShopSettings settings;
    private int databaseCallCount;

    public CategoryService(IMemoryCache cache, IOptions<ShopSettings> settings)
    {
        this.cache = cache;
        this.settings = settings.Value;
    }

    public int DatabaseCallCount
    {
        get
        {
            return Volatile.Read(ref databaseCallCount);
        }
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        List<string>? categories = await cache.GetOrCreateAsync(CategoriesCacheKey, async cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(settings.CategoryCacheMinutes);
            cacheEntry.Size = 1; // counts toward SizeLimit, so the cache can't grow forever
            return await LoadCategoriesFromDatabaseAsync();
        });
        return categories ?? new List<string>();
    }

    // Call this whenever categories are changed, so nobody sees old data.
    public void ClearCachedCategories()
    {
        cache.Remove(CategoriesCacheKey);
    }

    private async Task<List<string>> LoadCategoriesFromDatabaseAsync()
    {
        Interlocked.Increment(ref databaseCallCount);
        await Task.Delay(500); // pretend this is a slow SQL query
        return new List<string> { "Computers", "Furniture", "Lighting" };
    }
}
