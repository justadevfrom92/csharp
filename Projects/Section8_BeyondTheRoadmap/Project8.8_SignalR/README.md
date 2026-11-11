# Project 8.8 — SignalR (real-time chat)

| Folder | What |
|---|---|
| `SignalRChat/` | A chat hub with rooms, a browser chat page at `/`, and an HTTP announcement endpoint |
| `SignalRChat.Tests/` | Tests using the real .NET SignalR client against the in-memory app |

```bash
dotnet run --project SignalRChat
```
Open the page in two browser tabs with different names, join the same room, and chat.

```bash
curl -X POST "localhost:5000/announce?text=Server%20restarts%20at%206pm"
```

```bash
dotnet test SignalRChat.Tests
```

## SignalR vs raw WebSockets (Project 7.8)

7.8 handles bytes and frames by hand. **SignalR** does that for you:
- you **call methods** (`connection.invoke("SendMessage", room, text)`) instead of sending raw bytes;
- the server calls methods on clients (`Clients.Group(room).ReceiveMessage(...)`);
- it picks the best transport by itself (WebSockets → Server-Sent Events → long polling) and reconnects;
- **groups** (rooms), user tracking and scaling out (with Redis) are built in.

## The pieces

| Piece | Where | Why |
|---|---|---|
| `ChatHub : Hub<IChatClient>` | `Hubs/ChatHub.cs` | Methods clients can call: `JoinRoom`, `SendMessage` |
| `IChatClient` | `Clients/IChatClient.cs` | **Strongly-typed** client methods: `Clients.Group(room).ReceiveMessage(...)` is compiler-checked, no `SendAsync("ReceiveMesage")` typos |
| Groups | `Groups.AddToGroupAsync(Context.ConnectionId, room)` | A message to a room reaches its members and nobody else |
| `HubException` | `ChatHub` | Its message is sent to the caller ("Empty messages can't be sent."); other exceptions' details stay hidden (`EnableDetailedErrors = false`) |
| `IHubContext<ChatHub, IChatClient>` | `Endpoints/AnnouncementEndpoints.cs` | Push to clients from **outside** the hub (an HTTP endpoint, a background service) |
| `RequireChatUserNameMiddleware` | `Middlewares/` | Refuses a connection **before** it's made when `?user=` is missing or too long |
| JavaScript client | `wwwroot/lib/signalr/signalr.min.js` | Official `@microsoft/signalr` 10.0.11 from npm (MIT, see `SOURCE.txt`) |

**A hub object lives for ONE call.** SignalR creates a new `ChatHub` for every method call, so it must not keep
data in fields. Per-connection data (the user name, joined rooms) goes in `Context.Items`.

## A mistake the tests caught

My first version rejected bad user names by throwing in `OnConnectedAsync`. The Connect tests showed that this
does **not** refuse the connection: the client connects successfully and is then dropped, which is confusing
for the user and wasteful for the server. The check moved into `RequireChatUserNameMiddleware`, which answers
**400** to the connection request itself, so the client's `StartAsync()` fails with a clear reason.

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Connect | with a name → connected | 31-character name → refused with 400 before connecting | no name / only spaces → refused with 400 |
| Join room | room members see "Bob joined" | `General` (upper-case) → `HubException` | `""`, `has space`, a 27-character name → `HubException` |
| Send message | reaches room members (trimmed), **not** someone in another room | room not joined / 201 characters → `HubException` | empty or spaces-only text → `HubException` |
| Announce (`IHubContext`) | `POST /announce` → 202, every client receives it | a client in **no** room still receives it (it goes to all) | no text → 400, nobody receives anything |

Also checked in a real browser (two headless Chromium pages): Bob saw Alice's message, Alice got a clear error for
an empty message, and an HTTP announcement reached both.
