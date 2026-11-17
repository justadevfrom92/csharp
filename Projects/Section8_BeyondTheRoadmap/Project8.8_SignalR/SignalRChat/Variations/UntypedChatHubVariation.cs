using Microsoft.AspNetCore.SignalR;

namespace SignalRChat.Variations;

// An UNTYPED hub (plain Hub, not Hub<IChatClient> like the original ChatHub): client methods are called by
// NAME with SendAsync("ReceiveMessage", ...). Also shows WHO receives a message with each Clients.* target.
// Mapped at /hubs/variations/untyped (see Program.cs).
public class UntypedChatHubVariation : Hub
{
    // Clients.All: everyone connected, INCLUDING the sender.
    public Task SendToAll(string text)
    {
        return Clients.All.SendAsync("ReceiveMessage", "all", Context.ConnectionId[..4], text);
    }

    // Clients.Others: everyone EXCEPT the sender.
    public Task SendToOthers(string text)
    {
        return Clients.Others.SendAsync("ReceiveMessage", "others", Context.ConnectionId[..4], text);
    }

    // Clients.Caller: ONLY the sender (e.g. "your message was saved").
    public Task SendToCaller(string text)
    {
        return Clients.Caller.SendAsync("ReceiveMessage", "caller", Context.ConnectionId[..4], text);
    }

    // A TYPO in the method name: compiles, runs, no error anywhere - and no client ever receives it, because
    // nobody listens for "ReceiveMesage". Hub<IChatClient> (original) makes this a compile error.
    public Task SendWithTypo(string text)
    {
        return Clients.All.SendAsync("ReceiveMesage", "typo", Context.ConnectionId[..4], text);
    }
}
