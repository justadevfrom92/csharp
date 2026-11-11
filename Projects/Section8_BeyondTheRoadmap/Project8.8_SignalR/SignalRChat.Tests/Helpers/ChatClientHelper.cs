using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRChat.Tests.Helpers;

// One chat user connected to the in-memory app with the REAL .NET SignalR client.
// Everything the server sends to this user is collected in Received, so tests can check it.
public sealed class ChatClientHelper : IAsyncDisposable
{
    private readonly SemaphoreSlim newLine = new(0);

    private ChatClientHelper(HubConnection connection)
    {
        Connection = connection;
        Connection.On<string, string, string>("ReceiveMessage", (room, user, text) => Add($"[{room}] {user}: {text}"));
        Connection.On<string, string>("UserJoined", (room, user) => Add($"[{room}] {user} joined"));
        Connection.On<string>("ReceiveAnnouncement", text => Add($"ANNOUNCEMENT: {text}"));
    }

    public HubConnection Connection { get; }
    public ConcurrentQueue<string> Received { get; } = new();

    public static async Task<ChatClientHelper> ConnectAsync(WebApplicationFactory<Program> factory, string? userName)
    {
        string query = userName == null ? "" : "?user=" + Uri.EscapeDataString(userName);
        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/chat" + query), options =>
            {
                // Route the client's HTTP traffic into the in-memory test server.
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
        var client = new ChatClientHelper(connection);
        await connection.StartAsync();
        return client;
    }

    // Waits until a line containing the text arrives (or fails after 5 seconds).
    public async Task<string> WaitForAsync(string expectedPart)
    {
        DateTime giveUpAt = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < giveUpAt)
        {
            string? match = Received.FirstOrDefault(line => line.Contains(expectedPart));
            if (match != null)
            {
                return match;
            }
            await newLine.WaitAsync(TimeSpan.FromMilliseconds(200));
        }
        throw new TimeoutException($"Never received '{expectedPart}'. Got: {string.Join(" | ", Received)}");
    }

    private void Add(string line)
    {
        Received.Enqueue(line);
        newLine.Release();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}
