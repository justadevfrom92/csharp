using MvcProductCatalog.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton(TimeProvider.System);
// Singleton: the in-memory catalog must outlive individual requests to behave like a data store.
builder.Services.AddSingleton<IProductCatalogRepository, InMemoryProductCatalogRepository>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
// Re-executes the pipeline for 4xx/5xx responses with no body so users see a friendly page.
// The target action accepts every HTTP verb, so a failed POST (e.g. 400 anti-forgery) renders too.
app.UseStatusCodePagesWithReExecute("/status/{0}");
// Fixed culture so prices render as "$19.99" regardless of the server's OS locale.
app.UseRequestLocalization("en-US");

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// Attribute-routed controllers (ProductsController) are discovered by MapControllers semantics
// inside MapControllerRoute; conventional routing serves HomeController.
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>Exposed so integration/E2E tests can host the app with WebApplicationFactory.</summary>
public partial class Program;
