using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using SignalRChat.Clients;
using SignalRChat.Constants;

namespace SignalRChat.Hubs;

// A HUB: clients call its public methods (JoinRoom, SendMessage), and it calls methods on clients (IChatClient).
// SignalR picks the best transport by itself: WebSockets, then Server-Sent Events, then long polling.
// A new ChatHub object is created for EVERY call, so it must not keep state in fields.
public class ChatHub : Hub<IChatClient>
{
    // Key under which we remember, per connection, the user name and the rooms they joined.
    private const string UserNameKey = "user";

    // Runs when a client has connected: /hubs/chat?user=Alice
    // The name was already checked by Middlewares/RequireChatUserNameMiddleware BEFORE the connection was made
    // (throwing here would NOT refuse the connection - the client would connect and then just be dropped).
    public override Task OnConnectedAsync()
    {
        string userName = Context.GetHttpContext()!.Request.Query["user"].ToString();
        Context.Items[UserNameKey] = userName.Trim();
        return base.OnConnectedAsync();
    }

    public async Task JoinRoom(string room)
    {
        if (room == null || !Regex.IsMatch(room, ChatRuleConstants.RoomNamePattern))
        {
            throw new HubException("Room names are 1-20 characters: lower-case letters, digits and '-'.");
        }
        // GROUPS: a named set of connections. Sending to a group reaches everyone in it, nobody else.
        await Groups.AddToGroupAsync(Context.ConnectionId, room);
        Context.Items["room:" + room] = true;
        await Clients.Group(room).UserJoined(room, UserName);
    }

    public async Task SendMessage(string room, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new HubException("Empty messages can't be sent.");
        }
        if (text.Length > ChatRuleConstants.MaximumMessageLength)
        {
            throw new HubException($"Messages can be at most {ChatRuleConstants.MaximumMessageLength} characters.");
        }
        if (room == null || !Context.Items.ContainsKey("room:" + room))
        {
            throw new HubException($"Join the room '{room}' before sending to it.");
        }
        await Clients.Group(room).ReceiveMessage(room, UserName, text.Trim());
    }

    private string UserName
    {
        get
        {
            return (string)Context.Items[UserNameKey]!;
        }
    }
}
