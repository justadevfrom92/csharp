using System.Net.WebSockets;
using System.Text;

namespace RawWebSockets.Handlers;

// Talks to ONE connected browser/client over a raw WebSocket:
// every TEXT message it receives is sent back as "ECHO: <MESSAGE IN CAPITALS>".
public class EchoWebSocketHandler
{
    public const int MaximumMessageBytes = 4 * 1024;
    private readonly ILogger<EchoWebSocketHandler> logger;

    public EchoWebSocketHandler(ILogger<EchoWebSocketHandler> logger)
    {
        this.logger = logger;
    }

    public async Task HandleAsync(HttpContext context)
    {
        // A WebSocket starts life as a normal HTTP request asking to "upgrade". Anything else is a bad request.
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("This address only accepts WebSocket connections.");
            return;
        }

        using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
        logger.LogInformation("WebSocket connected");
        await RunMessageLoopAsync(socket, context.RequestAborted);
        logger.LogInformation("WebSocket finished: {Status}", socket.CloseStatus);
    }

    // THE FRAMING LOOP. One "message" can arrive in several "frames" (pieces). We keep reading frames into
    // a buffer until a frame says EndOfMessage = true, and only then handle the whole message.
    private async Task RunMessageLoopAsync(WebSocket socket, CancellationToken stopToken)
    {
        byte[] frameBuffer = new byte[1024];
        using var messageBuffer = new MemoryStream();

        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult frame = await socket.ReceiveAsync(frameBuffer, stopToken);

            if (frame.MessageType == WebSocketMessageType.Close)
            {
                // The client wants to stop: finish the close "handshake" politely.
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", stopToken);
                return;
            }
            if (frame.MessageType == WebSocketMessageType.Binary)
            {
                await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Only text messages are supported.", stopToken);
                return;
            }

            messageBuffer.Write(frameBuffer, 0, frame.Count);
            if (messageBuffer.Length > MaximumMessageBytes)
            {
                // Stop reading as soon as it's too big - don't let one client fill the server's memory.
                await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, $"Messages must be {MaximumMessageBytes} bytes or less.", stopToken);
                return;
            }
            if (!frame.EndOfMessage)
            {
                continue; // more frames of this message are coming
            }

            string message = Encoding.UTF8.GetString(messageBuffer.GetBuffer(), 0, (int)messageBuffer.Length);
            messageBuffer.SetLength(0); // ready for the next message

            byte[] reply = Encoding.UTF8.GetBytes("ECHO: " + message.ToUpperInvariant());
            await socket.SendAsync(reply, WebSocketMessageType.Text, endOfMessage: true, stopToken);
        }
    }
}
