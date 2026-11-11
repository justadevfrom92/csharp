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
