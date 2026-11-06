using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SharedCacheAndSettings.Services;

namespace SharedCacheAndSettings.Controllers;

[ApiController]
public class ShopController : ControllerBase
{
    private readonly CategoryService categoryService;
    private readonly ShopSettings settings;

    public ShopController(CategoryService categoryService, IOptions<ShopSettings> settings)
    {
        this.categoryService = categoryService;
        this.settings = settings.Value;
    }

    [HttpGet("/")]
    public IActionResult Settings()
    {
        return Ok(settings);
    }

    [HttpGet("/categories")]
    public async Task<IActionResult> Categories()
    {
        var timer = Stopwatch.StartNew();
        List<string> categories = await categoryService.GetCategoriesAsync();
        return Ok(new
        {
            categories,
            milliseconds = timer.ElapsedMilliseconds,
            databaseCallsSoFar = categoryService.DatabaseCallCount,
        });
    }

    [HttpPost("/categories/clear-cache")]
    public IActionResult ClearCache()
    {
        categoryService.ClearCachedCategories();
        return Ok("Category cache cleared. The next request will load from the database.");
    }
}
