using EfCoreQueryLogging.DbContexts;
using EfCoreQueryLogging.Interceptors;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog reads its levels from appsettings.json / appsettings.Development.json.
builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddSingleton<SqlQueryLoggingInterceptor>();

// AddDbContextPool reuses DbContext objects instead of creating a new one for every request.
builder.Services.AddDbContextPool<LibraryDbContext>((serviceProvider, options) =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("Library"));
    options.AddInterceptors(serviceProvider.GetRequiredService<SqlQueryLoggingInterceptor>());

    if (builder.Environment.IsDevelopment())
    {
        // Show real parameter values in EF's own logs. NEVER in production: they can contain passwords or personal data.
        options.EnableSensitiveDataLogging();
    }
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Apply migrations on startup to keep this demo one-command. (See Project 7.7 for why production should NOT do this.)
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.Migrate();
}

app.UseSerilogRequestLogging(); // one tidy log line per HTTP request
app.UseRouting();
app.MapControllerRoute("default", "{controller=Books}/{action=Index}/{id?}");
app.Run();
