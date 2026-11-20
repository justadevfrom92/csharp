using AsyncStreams.DbContexts;
using AsyncStreams.Endpoints;
using AsyncStreams.Services;
using AsyncStreams.Variations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// An in-memory SQLite database lives only while its connection is OPEN, so one connection is kept for the
// app's lifetime. Each app (and each test's app) gets its own private database.
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
builder.Services.AddDbContext<ShopDbContext>(options => options.UseSqlite(connection));
builder.Services.AddScoped<OrderStreamService>();

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.EnsureCreated(); // adds the HasData rows
}

app.MapStreamEndpoints();  // /ticks, /orders/big, /orders/big/sum, /orders/customers
app.MapBufferedTicks();    // /variations/ticks/buffered, /variations/ticks/broken

app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
