using OutputCachingCatalog.Middlewares;
using OutputCachingCatalog.Databases;
using OutputCachingCatalog.HealthChecks;

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

// IMiddleware classes must be registered in DI (a new one is created per request).
builder.Services.AddTransient<RequestTimingMiddleware>();

builder.Services.AddHealthChecks()
    .AddCheck<CatalogDatabaseHealthCheck>("catalog-database");

var app = builder.Build();

app.UseRouting();

// Custom middleware classes (see the Middlewares folder). They run for every request that is NOT
// short-circuited, in this order:
app.UseMiddleware<RequestTimingMiddleware>();      // IMiddleware style: times the request
app.UseMiddleware<FullPipelineHeaderMiddleware>(); // convention style: marks "went through the whole pipeline"

app.UseOutputCache(); // must come after UseRouting so it knows which endpoint was hit
app.MapHealthChecks("/health");

// /robots.txt -> RobotsController.Get().
// .ShortCircuit(): as soon as routing matches this URL, the endpoint runs and the response is sent.
// Every middleware after UseRouting (the two middleware classes above, output caching, and in a bigger app
// authentication/authorization) is SKIPPED. Crawlers ask for robots.txt constantly and it never needs
// a login, so there's no point running the rest of the pipeline for it.
app.MapControllerRoute(
    name: "robots",
    pattern: "robots.txt",
    defaults: new { controller = "Robots", action = "Get" })
   .ShortCircuit();

app.MapControllerRoute("default", "{controller=Catalog}/{action=Index}/{id?}");
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
