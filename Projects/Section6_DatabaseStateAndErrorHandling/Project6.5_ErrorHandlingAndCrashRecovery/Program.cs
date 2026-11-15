using Serilog.Formatting.Compact;
using Serilog;

// Logger is created FIRST, so even a crash during startup gets logged.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(), "logs/errors-.json", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    builder.Services.AddControllersWithViews();
    // Only for Variations/: an IExceptionHandler class, tried before the /Error/ServerError page.
    builder.Services.AddExceptionHandler<ErrorHandlingAndCrashRecovery.Variations.ApiErrorExceptionHandlerVariation>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        // Full stack trace in the browser. ONLY for the developer's own machine.
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // Production: catch every unhandled exception and show a friendly page instead.
        app.UseExceptionHandler("/Error/ServerError");
        app.UseHsts();
    }

    // 404, 403, etc. with no body: re-run the request as /Error/Status/404 to show a nice page.
    app.UseStatusCodePagesWithReExecute("/Error/Status/{0}");

    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "The app failed to start");
}
finally
{
    Log.CloseAndFlush(); // write any logs still in memory before the process ends
}
