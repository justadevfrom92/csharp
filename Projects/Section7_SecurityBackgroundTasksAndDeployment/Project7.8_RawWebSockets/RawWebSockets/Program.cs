using RawWebSockets.Handlers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<EchoWebSocketHandler>();

var app = builder.Build();

app.UseDefaultFiles(); // "/" -> wwwroot/index.html (a small page to try the WebSocket in a browser)
app.UseStaticFiles();

// Turns on WebSocket support. KeepAliveInterval: the server pings every 30 s so idle connections
// aren't silently dropped by proxies and firewalls.
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

// ws://host/ws/echo is handled by EchoWebSocketHandler.
app.Map("/ws/echo", async (HttpContext context, EchoWebSocketHandler handler) =>
{
    await handler.HandleAsync(context);
});

app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
