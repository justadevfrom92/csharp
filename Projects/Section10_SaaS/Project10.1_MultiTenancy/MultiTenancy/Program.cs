using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MultiTenancy.Contexts;
using MultiTenancy.DbContexts;
using MultiTenancy.Endpoints;
using MultiTenancy.Middlewares;
using MultiTenancy.Services;
using MultiTenancy.Stores;

var builder = WebApplication.CreateBuilder(args);

// In-memory SQLite lives while its connection is open: one connection for the app's lifetime (as in 5.4).
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

builder.Services.AddSingleton<TenantStore>();
builder.Services.AddScoped<TenantContext>();   // one per request
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
builder.Services.AddScoped<ProjectService>();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated(); // adds the seed rows
}

app.UseMiddleware<TenantResolutionMiddleware>(); // before the endpoints: every request needs a tenant
app.MapProjectEndpoints();

app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
