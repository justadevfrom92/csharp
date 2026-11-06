using FeatureFlags.Services;
using FeatureFlags.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FeatureFlagSettings>(builder.Configuration.GetSection("FeatureFlags"));
builder.Services.AddSingleton<FeatureFlagService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Shop}/{action=Index}/{id?}");
app.Run();
