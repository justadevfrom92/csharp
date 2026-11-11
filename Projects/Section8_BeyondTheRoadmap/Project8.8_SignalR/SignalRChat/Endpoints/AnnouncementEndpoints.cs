using Microsoft.AspNetCore.SignalR;
using SignalRChat.Clients;
using SignalRChat.Hubs;

namespace SignalRChat.Endpoints;

public static class AnnouncementEndpoints
{
    // POST /announce?text=... - sends to EVERY connected client from OUTSIDE the hub.
    // IHubContext<ChatHub, IChatClient> lets any code (an endpoint, a background service) push to clients.
    public static void MapAnnouncementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/announce", async (string? text, IHubContext<ChatHub, IChatClient> chatHub) =>
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Results.Problem(statusCode: 400, title: "Send ?text=<announcement>.");
            }
            await chatHub.Clients.All.ReceiveAnnouncement(text.Trim());
            return Results.Accepted();
        });
    }
}
