# Project 7.8 — Raw WebSockets

| Folder | What |
|---|---|
| `RawWebSockets/` | Web app: `ws://.../ws/echo` + a test page at `/` |
| `RawWebSockets.Tests/` | xUnit tests using a real WebSocket client against the in-memory app |

```bash
dotnet run --project RawWebSockets
```
Open the URL it prints, type a message, press Send.

```bash
dotnet test RawWebSockets.Tests
```

## What a WebSocket is

Normal HTTP is "ask → answer → done". A WebSocket starts as an HTTP request that asks to **upgrade**; after that
the same connection stays open and **both sides can send messages at any time** (chat, live prices, games).
"Raw" means we handle the bytes ourselves. (SignalR, Project 8.8, does all this for you.)

## How the server works (`Handlers/EchoWebSocketHandler.cs`)

1. `app.UseWebSockets(...)` turns WebSocket support on. `KeepAliveInterval` makes the server ping idle
   connections so proxies don't silently drop them.
2. `context.WebSockets.IsWebSocketRequest` — a normal HTTP request to this address gets **400**.
3. `AcceptWebSocketAsync()` completes the upgrade and gives us a `WebSocket`.
4. **The framing loop.** One *message* can arrive as several *frames*. `ReceiveAsync` returns one frame at a time;
   we append each frame to a buffer until a frame has `EndOfMessage = true`, then handle the whole message.
5. Reply with `SendAsync(..., WebSocketMessageType.Text, endOfMessage: true)`.
6. A `Close` frame from the client → we answer with `CloseAsync(NormalClosure)` (the close **handshake**).

## Rules, and why

| Rule | Close code | Why |
|---|---|---|
| Messages over 4 KB | `MessageTooBig` (1009) | Without a limit, one client could send an endless message and fill the server's memory. We check **while** reading, not after. |
| Binary messages | `InvalidMessageType` (1003) | This endpoint only understands text. Closing with a clear code tells the client exactly why. |
| Not a WebSocket request | HTTP 400 | The address can't do anything useful for a normal GET. |

**Why a `MemoryStream` buffer and a fixed 1 KB frame buffer:** frames can be any size, so we read in fixed
chunks and collect them; the size check is on the collected total.

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Echo | `hello` → `ECHO: HELLO`; a message sent as **3 frames** → **one** echo `ECHO: HELLO BIG WORLD` | 4 KB + 1 byte → closed with `MessageTooBig`; exactly 4 KB → still echoed (the edge) | binary frame → closed with `InvalidMessageType`; plain HTTP GET → 400 |

Also checked by hand with a real browser: headless Chromium opened the page, sent "hi from chromium" over a
real `ws://` connection to Kestrel, and got `ECHO: HI FROM CHROMIUM` back.

## Code snippets

**Turn on WebSockets and map an address to a handler**, from `RawWebSockets/Program.cs`

```csharp
builder.Services.AddSingleton<EchoWebSocketHandler>();

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.Map("/ws/echo", async (HttpContext context, EchoWebSocketHandler handler) =>
{
    await handler.HandleAsync(context);
});
```

**Accept the connection (only real WebSocket requests)**, from `Handlers/EchoWebSocketHandler.cs`

```csharp
if (!context.WebSockets.IsWebSocketRequest)
{
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    return;
}
using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
```

**The receive loop: join frames into one message, handle close, limit size, reply**

```csharp
byte[] frameBuffer = new byte[1024];
using var messageBuffer = new MemoryStream();
while (socket.State == WebSocketState.Open)
{
    WebSocketReceiveResult frame = await socket.ReceiveAsync(frameBuffer, stopToken);
    if (frame.MessageType == WebSocketMessageType.Close)
    {
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", stopToken);
        return;
    }
    messageBuffer.Write(frameBuffer, 0, frame.Count);
    if (messageBuffer.Length > MaximumMessageBytes)
    {
        await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Too big.", stopToken);
        return;
    }
    if (!frame.EndOfMessage)
    {
        continue; // more frames of this message are coming
    }
    string message = Encoding.UTF8.GetString(messageBuffer.GetBuffer(), 0, (int)messageBuffer.Length);
    messageBuffer.SetLength(0);
    byte[] reply = Encoding.UTF8.GetBytes("ECHO: " + message.ToUpperInvariant());
    await socket.SendAsync(reply, WebSocketMessageType.Text, endOfMessage: true, stopToken);
}
```

**The browser side**, from `wwwroot/index.html`

```js
const socket = new WebSocket(scheme + location.host + "/ws/echo");
socket.onmessage = event => log("received: " + event.data);
socket.send("hello");
```
