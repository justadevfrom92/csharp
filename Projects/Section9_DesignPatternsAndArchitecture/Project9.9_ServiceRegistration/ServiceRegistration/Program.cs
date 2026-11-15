using ServiceRegistration.Endpoints;
using ServiceRegistration.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ---------- Our own registrations, grouped in extension methods (Extensions/) ----------
builder.Services.RegisterAllExistingJobs();   // every IJob class in this project + the runner that runs them
builder.Services.AddShopServices();           // lifetimes, keyed, options, factory, open generic, many, decorator, TryAdd

// ---------- Framework registrations ----------
builder.Services.AddProblemDetails();         // errors as application/problem+json
builder.Services.AddHealthChecks();           // GET /health
builder.Services.AddHttpClient();             // IHttpClientFactory (see 8.3)

// ---------- Let the container check itself ----------
// ValidateScopes: error if a scoped service is taken from the ROOT provider (it would live forever).
// ValidateOnBuild: at startup, check that every registered service CAN be created (missing dependency = crash now,
// not on the first request that happens to need it). Both are on in Development by default; here: always.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

var app = builder.Build();
app.UseExceptionHandler();
app.MapHealthChecks("/health");
app.MapDemoEndpoints();
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
