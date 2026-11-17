# Project 8.9 — gRPC

| Folder | What |
|---|---|
| `StockGrpc/` | A gRPC stock service: look up, reserve, and watch stock levels live |
| `StockGrpc.Tests/` | Tests with a real generated gRPC client against the in-memory server |

```bash
dotnet run --project StockGrpc --urls http://localhost:5089
```

```bash
dotnet test StockGrpc.Tests
```

To call it from another .NET app: add `Grpc.Net.Client`, `Google.Protobuf` and `Grpc.Tools`, include the same
`.proto` file with `<Protobuf Include="path/to/stock.proto" GrpcServices="Client" />`, then:

```csharp
using var channel = GrpcChannel.ForAddress("http://localhost:5089");
var client = new StockService.StockServiceClient(channel);
StockReply reply = await client.GetStockAsync(new GetStockRequest { Sku = "KB-01" });
```

(Checked for real: a separate client generated only from the `.proto` called the running server over HTTP/2 —
get, reserve, and a `FailedPrecondition` for reserving too many.)

## gRPC vs a JSON API (Project 8.1)

| | JSON API (Minimal APIs) | gRPC |
|---|---|---|
| Contract | OpenAPI document, generated from code | **`.proto` file first**; code generated from it on both sides |
| Format | JSON text | **Protobuf** binary — smaller and faster to read/write |
| Transport | HTTP/1.1 or HTTP/2 | **HTTP/2** required (multiplexing, streaming) |
| Streaming | not built in | **built in**: server, client and two-way streams |
| Browsers | work directly | need gRPC-Web |
| Best for | public APIs, browsers | **service-to-service** calls inside a system, mobile apps, high volume |

## The pieces

| Piece | Where |
|---|---|
| The contract: messages, field numbers, `service StockService` with 3 methods | `Protos/stock.proto` |
| Code generation (`Grpc.Tools`), `GrpcServices="Both"` | `StockGrpc.csproj` |
| Implementation: `StockGrpcService : StockService.StockServiceBase` | `Services/StockGrpcService.cs` |
| **Unary** calls: `GetStock`, `ReserveStock` | one request → one reply |
| **Server streaming**: `WatchStock` | sends the current level, then a new message after every change, until the client cancels |
| HTTP/2 without TLS for local runs | `appsettings.json` → `Kestrel:EndpointDefaults:Protocols = Http2` |

**Field numbers** (`string sku = 1;`) are what's sent on the wire, not the names. Never change or reuse one once
it's in use; add new fields with new numbers. That's how old and new clients keep working together.

**Errors are gRPC status codes**, thrown as `RpcException`:

| Situation | Status code | HTTP equivalent |
|---|---|---|
| Malformed SKU, quantity 0 | `InvalidArgument` | 400 |
| SKU doesn't exist | `NotFound` | 404 |
| Valid request, not enough stock | `FailedPrecondition` | 409 / 422 |
| Client stopped waiting | `Cancelled` | — |

**Streaming without leaks:** `WatchStock` subscribes to the store's `StockChanged` event and passes changes to the
stream through a `Channel` (events fire on other threads). The `finally` always unsubscribes, so a finished or
cancelled stream doesn't leave a handler behind forever.

**Why `GrpcServices="Both"`:** the test project references the server project, so it reuses the client class
generated there. Generating the messages twice (once per project) would create duplicate classes. A separate
client app would include the `.proto` with `GrpcServices="Client"`.

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `GetStock` | `KB-01` → Keyboard, 10 | `ZZ-99` (well-formed, doesn't exist) → `NotFound` | `""`, `kb-01`, `KEYBOARD` → `InvalidArgument` |
| `ReserveStock` | reserve all 3 → 0 left, and `GetStock` agrees | reserve 4 of 3 → `FailedPrecondition` "Only 3", nothing changed | quantity 0 / -2, SKU `bad` → `InvalidArgument` |
| `WatchStock` (stream) | 10 now → 6 after reserving 4 → 5 after reserving 1 | a change to a **different** product sends nothing | unknown SKU → `NotFound`; malformed → `InvalidArgument` |

Each test starts a new app (fresh stock levels) and talks to it with the real generated client.

## Code snippets

**The contract: a `.proto` file**, from `StockGrpc/Protos/stock.proto`

```proto
syntax = "proto3";
option csharp_namespace = "StockGrpc.Protos";

service StockService {
  rpc GetStock (GetStockRequest) returns (StockReply);                // one request -> one reply
  rpc WatchStock (WatchStockRequest) returns (stream StockReply);     // one request -> a stream of replies
}

message StockReply {
  string sku = 1;   // field numbers: never reuse or change them
  string name = 2;
  int32 quantity = 3;
}
```

**Generate C# from it**, from `StockGrpc/StockGrpc.csproj`

```xml
<Protobuf Include="Protos/stock.proto" GrpcServices="Both" />
```

**Host the service**, from `Program.cs`

```csharp
builder.Services.AddGrpc();
app.MapGrpcService<StockGrpcService>();
```

**Implement it: override the generated base class; errors are gRPC status codes** (simplified from `Services/StockGrpcService.cs`)

```csharp
public class StockGrpcService : StockService.StockServiceBase
{
    public override Task<StockReply> GetStock(GetStockRequest request, ServerCallContext context)
    {
        var item = store.Find(request.Sku);
        if (item == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"No product with SKU '{request.Sku}'."));
        }
        return Task.FromResult(new StockReply { Sku = request.Sku, Name = item.Value.Name, Quantity = item.Value.Quantity });
    }
}
```

**Server streaming: keep writing until the client cancels**

```csharp
public override async Task WatchStock(WatchStockRequest request, IServerStreamWriter<StockReply> responseStream, ServerCallContext context)
{
    await responseStream.WriteAsync(CurrentStock(request.Sku));
    await foreach (string _ in changes.Reader.ReadAllAsync(context.CancellationToken))
    {
        await responseStream.WriteAsync(CurrentStock(request.Sku));
    }
}
```

**A C# client (here, in the tests)**, from `StockGrpc.Tests/`

```csharp
GrpcChannel channel = GrpcChannel.ForAddress(factory.Server.BaseAddress, new GrpcChannelOptions
{
    HttpHandler = factory.Server.CreateHandler(),
});
var client = new StockService.StockServiceClient(channel);

StockReply reply = await client.GetStockAsync(new GetStockRequest { Sku = "KB-01" });

RpcException error = await Assert.ThrowsAsync<RpcException>(() => client.GetStockAsync(new GetStockRequest { Sku = "ZZ-99" }).ResponseAsync);
Assert.Equal(StatusCode.NotFound, error.StatusCode);
```

## Variations

Other ways to **call** the service with the generated client, in `StockGrpc/Variations/StockClientVariation.cs`.
`StockGrpc.Tests/VariationsTest.cs` runs them against the real service in memory.

| Way | Result | The difference |
|---|---|---|
| `await client.GetStockAsync(request)` (original) | the reply | the normal choice |
| `client.GetStock(request)` (no `Async`) | same reply | **blocks** the calling thread until the answer comes |
| `GetStockAsync(request, deadline: DateTime.UtcNow.AddSeconds(5))` | same reply | gives up after the time limit; the server's `context.CancellationToken` is cancelled too |
| `GetStockAsync(request, new Metadata { { "x-correlation-id", "..." } })` | same reply + response headers | **metadata** = gRPC's headers (correlation ids, auth tokens) |
| bad SKU `"bad"`, any style | `RpcException` `InvalidArgument` | the status code doesn't depend on the call style |

**Reading the `WatchStock` stream** (Mouse stock 3, then one reserved)

| Way | Received | The difference |
|---|---|---|
| `await foreach (var reply in call.ResponseStream.ReadAllAsync())` (original) | 3, 2 | shortest |
| `while (await call.ResponseStream.MoveNext()) { ... Current ... }` | 3, 2 | older style, same messages |
| either, with `deadline: now + 300 ms` on a stream that never ends | `RpcException` **`DeadlineExceeded`** | without a deadline (or a cancellation token) the loop waits forever |

**Exposing the same service other ways** (documented):

| Way | Who can call it |
|---|---|
| plain gRPC over HTTP/2 (original) | gRPC clients (.NET, Go, Java, Python...) — not browsers |
| gRPC-Web (`Grpc.AspNetCore.Web`, `app.UseGrpcWeb()`) | browsers too, through a gRPC-Web JavaScript client |
| JSON transcoding (`Microsoft.AspNetCore.Grpc.JsonTranscoding` + `google.api.http` options in the `.proto`) | anything that speaks JSON over HTTP: `GET /v1/stock/KB-01` |
| a JSON API alongside (8.1) | anyone; two APIs to maintain |
