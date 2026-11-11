namespace SignalRChat.Clients;

// STRONGLY-TYPED hub client: the methods the SERVER can call on every connected browser/app.
// Hub<IChatClient> means "Clients.Group(...).ReceiveMessage(...)" is checked by the compiler -
// no more typo-prone strings like Clients.All.SendAsync("ReceiveMesage", ...).
public interface IChatClient
{
    Task ReceiveMessage(string room, string user, string text);
    Task UserJoined(string room, string user);
    Task ReceiveAnnouncement(string text);
}
