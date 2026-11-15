using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace RawWebSockets.Variations;

// Two other ways to write the echo loop of EchoWebSocketHandler (original). Mapped in Program.cs at
// /ws/variations/naive and /ws/variations/pooled.
public static class WebSocketEchoVariation
{
    // NAIVE: treats every FRAME as a whole message (ignores EndOfMessage). Short messages look fine, but a
    // message sent in several frames is echoed back in PIECES - the bug the original's framing loop prevents.
    public static async Task RunNaiveAsync(WebSocket socket, CancellationToken stopToken)
    {
        byte[] buffer = new byte[1024];
        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult frame = await socket.ReceiveAsync(buffer, stopToken);
            if (frame.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", stopToken);
                return;
            }
            string piece = Encoding.UTF8.GetString(buffer, 0, frame.Count);
            await socket.SendAsync(Encoding.UTF8.GetBytes("ECHO: " + piece.ToUpperInvariant()), WebSocketMessageType.Text, true, stopToken);
        }
    }

    // POOLED: same framing logic as the original, but the receive buffer is BORROWED from ArrayPool and uses the
    // Memory<byte> overload of ReceiveAsync (returns a struct: no allocation per frame). Same answers; less garbage
    // when thousands of sockets are open.
    public static async Task RunPooledAsync(WebSocket socket, CancellationToken stopToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            using var message = new MemoryStream();
            while (socket.State == WebSocketState.Open)
            {
                ValueWebSocketReceiveResult frame = await socket.ReceiveAsync(buffer.AsMemory(), stopToken);
                if (frame.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", stopToken);
                    return;
                }
                message.Write(buffer, 0, frame.Count);
                if (!frame.EndOfMessage)
                {
                    continue;
                }
                string text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                message.SetLength(0);
                await socket.SendAsync(Encoding.UTF8.GetBytes("ECHO: " + text.ToUpperInvariant()), WebSocketMessageType.Text, true, stopToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // Maps both variations. Called from Program.cs: app.MapWebSocketVariations();
    public static void MapWebSocketVariations(this IEndpointRouteBuilder app)
    {
        app.Map("/ws/variations/naive", async (HttpContext context) =>
        {
            using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            await RunNaiveAsync(socket, context.RequestAborted);
        });
        app.Map("/ws/variations/pooled", async (HttpContext context) =>
        {
            using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            await RunPooledAsync(socket, context.RequestAborted);
        });
    }
}
