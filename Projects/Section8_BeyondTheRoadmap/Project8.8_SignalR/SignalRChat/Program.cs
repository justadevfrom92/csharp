using SignalRChat.Endpoints;
using SignalRChat.Hubs;
using SignalRChat.Middlewares;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options =>
{
    // A HubException's message ALWAYS reaches the client (that's how we report "empty message" etc.).
    // false = the details of any OTHER exception (a bug, a database error) stay hidden from clients.
    options.EnableDetailedErrors = false;
});
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseDefaultFiles();          // "/" -> wwwroot/index.html (a small chat page)
app.UseStaticFiles();
app.UseMiddleware<RequireChatUserNameMiddleware>(); // refuses /hubs/chat connections without a valid ?user=
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<SignalRChat.Variations.UntypedChatHubVariation>("/hubs/variations/untyped"); // other ways, for comparison (Variations/)
app.MapAnnouncementEndpoints();
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
