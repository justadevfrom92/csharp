using MvcSidebarViewComponentAndDateBinder.ModelBinding;
using MvcSidebarViewComponentAndDateBinder.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(mvcOptions =>
{
    // Index 0: must run before the framework's built-in simple-type binder would claim DateOnly.
    mvcOptions.ModelBinderProviders.Insert(0, new CustomIsoDateFormatModelBinderProvider());
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRecentActivityFeedService, InMemoryRecentActivityFeedService>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRequestLocalization("en-US");
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;
