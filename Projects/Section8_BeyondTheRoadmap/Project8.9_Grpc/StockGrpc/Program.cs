using StockGrpc.Services;
using StockGrpc.Stores;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddSingleton<StockStore>();

var app = builder.Build();
app.MapGrpcService<StockGrpcService>();
app.MapGet("/", () => "This is a gRPC service. Call it with a gRPC client (see the README).");
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
