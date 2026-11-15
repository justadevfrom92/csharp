using SharedCacheAndSettings.Services;
using SharedCacheAndSettings.Settings;

var builder = WebApplication.CreateBuilder(args);

// 1. Settings: bind the "Shop" section once, check it, and stop the app at startup if it's wrong.
builder.Services.AddOptions<ShopSettings>()
    .Bind(builder.Configuration.GetSection("Shop"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 2. One shared memory cache with a size limit (every entry must set Size).
int maximumCachedItems = builder.Configuration.GetValue<int>("Shop:MaximumCachedItems");
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = maximumCachedItems;
});

// 3. Singleton: one CategoryService for the whole app (it only holds the cache and settings).
builder.Services.AddSingleton<CategoryService>();

// Only for Variations/CacheStampedeVariation.cs: HybridCache, a newer cache that also prevents "stampedes".
builder.Services.AddHybridCache();

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
