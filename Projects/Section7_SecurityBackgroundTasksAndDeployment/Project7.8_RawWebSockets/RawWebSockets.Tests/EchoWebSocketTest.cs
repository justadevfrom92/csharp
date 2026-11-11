using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using RawWebSockets.Handlers;

namespace RawWebSockets.Tests;

// PositivePositive - valid text message(s) -> echoed back in capitals
// PositiveNegative - a well-formed text message that is too big -> server closes with MessageTooBig
// NegativeNegative - wrong kind of input (binary frame, plain HTTP request) -> rejected
public class EchoWebSocketTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public EchoWebSocketTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    // A real WebSocket connection to the in-memory server.
    private async Task<WebSocket> ConnectAsync()
    {
        WebSocketClient client = factory.Server.CreateWebSocketClient();
        return await client.ConnectAsync(new Uri(factory.Server.BaseAddress, "ws/echo"), CancellationToken.None);
    }

    private static Task SendTextAsync(WebSocket socket, string text, bool endOfMessage = true)
    {
        return socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, endOfMessage, CancellationToken.None);
    }

    private static async Task<(WebSocketReceiveResult Result, string Text)> ReceiveAsync(WebSocket socket)
    {
        byte[] buffer = new byte[8 * 1024];
        WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        return (result, Encoding.UTF8.GetString(buffer, 0, result.Count));
    }

    [Fact]
    public async Task Echo_PositivePositive_TextMessage_IsEchoedInCapitals()
    {
        using WebSocket socket = await ConnectAsync();

        await SendTextAsync(socket, "hello");
        (WebSocketReceiveResult result, string text) = await ReceiveAsync(socket);

        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        Assert.Equal("ECHO: HELLO", text);
    }

    [Fact]
    public async Task Echo_PositivePositive_MessageSplitIntoFrames_IsJoinedIntoOneEcho()
    {
        using WebSocket socket = await ConnectAsync();

        // One message, sent as three frames: only the last one says EndOfMessage.
        await SendTextAsync(socket, "hello ", endOfMessage: false);
        await SendTextAsync(socket, "big ", endOfMessage: false);
        await SendTextAsync(socket, "world", endOfMessage: true);
        (_, string text) = await ReceiveAsync(socket);

        Assert.Equal("ECHO: HELLO BIG WORLD", text);
    }

    [Fact]
    public async Task Echo_PositiveNegative_TextOverSizeLimit_ServerClosesWithMessageTooBig()
    {
        using WebSocket socket = await ConnectAsync();

        await SendTextAsync(socket, new string('a', EchoWebSocketHandler.MaximumMessageBytes + 1));
        (WebSocketReceiveResult result, _) = await ReceiveAsync(socket);

        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        Assert.Equal(WebSocketCloseStatus.MessageTooBig, result.CloseStatus);
    }

    [Fact]
    public async Task Echo_PositiveNegative_TextExactlyAtSizeLimit_IsStillEchoed()
    {
        using WebSocket socket = await ConnectAsync();

        await SendTextAsync(socket, new string('a', EchoWebSocketHandler.MaximumMessageBytes));
        (WebSocketReceiveResult result, string text) = await ReceiveAsync(socket);

        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        Assert.StartsWith("ECHO: AAAA", text);
    }

    [Fact]
    public async Task Echo_NegativeNegative_BinaryFrame_ServerClosesWithInvalidMessageType()
    {
        using WebSocket socket = await ConnectAsync();

        await socket.SendAsync(new byte[] { 1, 2, 3 }, WebSocketMessageType.Binary, true, CancellationToken.None);
        (WebSocketReceiveResult result, _) = await ReceiveAsync(socket);

        Assert.Equal(WebSocketMessageType.Close, result.MessageType);
        Assert.Equal(WebSocketCloseStatus.InvalidMessageType, result.CloseStatus);
    }

    [Fact]
    public async Task Echo_NegativeNegative_PlainHttpRequest_Returns400()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/ws/echo");

        Assert.Equal(400, (int)response.StatusCode);
    }
}
