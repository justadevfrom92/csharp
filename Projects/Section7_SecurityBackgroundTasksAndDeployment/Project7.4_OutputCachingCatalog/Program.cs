using OutputCachingCatalog.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CatalogDatabase>();
builder.Services.AddControllersWithViews();

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Catalog", policy =>
    {
        policy.Expire(TimeSpan.FromSeconds(60)); // safety net: never older than 60 s
        policy.Tag("catalog");                   // lets us clear all catalog pages at once
        policy.SetVaryByQuery("page");           // ?page=1 and ?page=2 are cached separately
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<CatalogDatabaseHealthCheck>("catalog-database");

var app = builder.Build();

app.UseRouting();
app.UseOutputCache(); // must come after UseRouting so it knows which endpoint was hit
app.MapHealthChecks("/health");
app.MapControllerRoute("default", "{controller=Catalog}/{action=Index}/{id?}");
app.Run();
