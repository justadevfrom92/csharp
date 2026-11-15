using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RawWebSockets.Tests;

// The original echo (/ws/echo) vs the naive and pooled versions (RawWebSockets/Variations/WebSocketEchoVariation.cs).
public class VariationsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public VariationsTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private async Task<WebSocket> ConnectAsync(string path)
    {
        return await factory.Server.CreateWebSocketClient().ConnectAsync(new Uri(factory.Server.BaseAddress, path), CancellationToken.None);
    }

    // Sends ONE message split into several frames (endOfMessage = false until the last one).
    private static async Task SendInFramesAsync(WebSocket socket, params string[] pieces)
    {
        for (int index = 0; index < pieces.Length; index++)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(pieces[index]), WebSocketMessageType.Text, index == pieces.Length - 1, CancellationToken.None);
        }
    }

    private static async Task<string> ReceiveMessageAsync(WebSocket socket)
    {
        var buffer = new byte[8192];
        using var message = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            message.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);
        return Encoding.UTF8.GetString(message.ToArray());
    }

    [Theory]
    [InlineData("ws/echo")]
    [InlineData("ws/variations/naive")]
    [InlineData("ws/variations/pooled")]
    public async Task Echo_PositivePositive_OneFrameMessage_AllThreeAgree(string path)
    {
        using WebSocket socket = await ConnectAsync(path);

        await SendInFramesAsync(socket, "hello");

        Assert.Equal("ECHO: HELLO", await ReceiveMessageAsync(socket));
    }

    [Theory]
    [InlineData("ws/echo")]
    [InlineData("ws/variations/pooled")]
    public async Task Echo_PositiveNegative_MessageInThreeFrames_ProperFramingEchoesItOnce(string path)
    {
        using WebSocket socket = await ConnectAsync(path);

        await SendInFramesAsync(socket, "one ", "two ", "three");

        Assert.Equal("ECHO: ONE TWO THREE", await ReceiveMessageAsync(socket));
    }

    [Fact]
    public async Task Echo_PositiveNegative_MessageInThreeFrames_NaiveVersionEchoesThreePieces()
    {
        using WebSocket socket = await ConnectAsync("ws/variations/naive");

        await SendInFramesAsync(socket, "one ", "two ", "three");

        Assert.Equal("ECHO: ONE ", await ReceiveMessageAsync(socket));   // each FRAME treated as a message
        Assert.Equal("ECHO: TWO ", await ReceiveMessageAsync(socket));
        Assert.Equal("ECHO: THREE", await ReceiveMessageAsync(socket));
    }

    [Fact]
    public async Task Echo_NegativeNegative_PlainHttpRequestToAVariation_IsRejected()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/ws/variations/pooled");

        // The variations skip the IsWebSocketRequest check, so AcceptWebSocketAsync throws -> 500. The original checks first -> 400.
        Assert.Equal(500, (int)response.StatusCode);
        Assert.Equal(400, (int)(await factory.CreateClient().GetAsync("/ws/echo")).StatusCode);
    }
}
