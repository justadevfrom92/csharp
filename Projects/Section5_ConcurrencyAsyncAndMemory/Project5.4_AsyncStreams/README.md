# Project 5.4 — Async Streams (`IAsyncEnumerable`, `await foreach`)

Items that arrive **over time**: price ticks made with `await` between them, database rows read one by one, and an
API endpoint that sends JSON **piece by piece**. 2.2 shows the normal (`IEnumerable`) version; this adds `async`.

```bash
cd AsyncStreams
dotnet run
```

```bash
cd AsyncStreams.Tests
dotnet test
```

Try it (port 5054):

```bash
curl -N "http://localhost:5054/ticks?count=5&delayMs=1000"
```

Each tick appears about a second after the last one: `-N` turns off curl's buffering so you can watch.

## `IEnumerable` vs `IAsyncEnumerable`

| | Normal | Async |
|---|---|---|
| Type | `IEnumerable<T>` | `IAsyncEnumerable<T>` |
| Loop | `foreach` | `await foreach` |
| Iterator method | `IEnumerable<T> M() { yield return x; }` | `async IAsyncEnumerable<T> M() { await ...; yield return x; }` |
| Cursor | `MoveNext()` → `bool` | `MoveNextAsync()` → `ValueTask<bool>` |
| Cleanup | `Dispose()` | `DisposeAsync()` |
| LINQ | `System.Linq` | `System.Linq.AsyncEnumerable` — **built into .NET 10** (earlier: the `System.Linq.Async` package) |
| Waiting between items | blocks the thread | frees the thread |

## What's in it

| What | File / URL |
|---|---|
| async iterator with `[EnumeratorCancellation]` | `AsyncStreams/Generators/PriceTickGenerator.cs` |
| EF Core `AsAsyncEnumerable()`, `await foreach` sum, async LINQ `Where/Select/Take/ToListAsync` | `AsyncStreams/Services/OrderStreamService.cs` |
| streamed endpoints | `GET /ticks`, `/orders/big`, `/orders/big/sum`, `/orders/customers` (`Endpoints/StreamEndpoints.cs`) |

## Things the tests showed

- **Streaming really streams:** with 3 ticks 800 ms apart, the client got the **first after ~800 ms**, the last after ~2,400 ms.
  (Seen with curl too: the first tick printed at 0.9 s.)
- The **buffered** version (`ToListAsync()` then `Ok(list)`) sends **nothing, not even the status code**, until all 3 are made.
- **An error halfway can't become a 500:** the 200 status and the first items are already sent. The client gets
  `200`, then the body is cut off (`HttpRequestException` while reading; curl printed `[{"number":1,...}` and stopped).
  Check everything that can fail **before** the first item.
- An already-cancelled token does **not** throw at the call — only at the first `MoveNextAsync()`, like 2.2's late errors.

## Code snippets

**An async iterator**

```csharp
public static async IAsyncEnumerable<PriceTickModel> StreamAsync(
    string symbol, int count, TimeSpan delay,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    for (int number = 1; number <= count; number++)
    {
        await Task.Delay(delay, cancellationToken);
        yield return new PriceTickModel(number, symbol, price);
    }
}
```

**Consuming it, with a token**

```csharp
await foreach (PriceTickModel tick in PriceTickGenerator.StreamAsync("ACME", 10, delay).WithCancellation(token))
{
}
```

**Database rows one at a time (EF Core)**

```csharp
IAsyncEnumerable<OrderModel> orders = database.Orders.Where(order => order.Total >= 100m).AsAsyncEnumerable();
await foreach (OrderModel order in orders)
{
}
```

**Async LINQ (.NET 10, no package)**

```csharp
List<string> names = await orders.Where(order => order.Customer.Length <= 3)
                                 .Select(order => order.Customer)
                                 .Take(2)
                                 .ToListAsync(cancellationToken);
```

**A streaming endpoint**

```csharp
app.MapGet("/ticks", (CancellationToken cancellationToken) =>
    PriceTickGenerator.StreamAsync("ACME", 5, TimeSpan.FromMilliseconds(500), cancellationToken));
```

**Reading a stream on the client**

```csharp
await foreach (PriceTickModel? tick in httpClient.GetFromJsonAsAsyncEnumerable<PriceTickModel>("/ticks"))
{
}
```

## Variations

In `AsyncStreams/Variations/`, checked by `AsyncStreams.Tests/VariationsTest.cs` and `StreamEndpointsTest.cs`.

**Cancelling an async iterator** (`CancellationVariation`, looped with an already-cancelled `.WithCancellation(token)`)

| Iterator | Result | The difference |
|---|---|---|
| token parameter **with** `[EnumeratorCancellation]` (original) | `OperationCanceledException` | works as an argument AND through `WithCancellation` |
| **no** token parameter | **all 3 items** — the token is silently ignored | nothing can stop it but `break` |
| token checked by hand, `yield break` | empty list, **no exception** | quiet: callers can't tell "cancelled" from "finished" |

A token parameter **without** the attribute gives compiler warning CS8425; `WithCancellation` would not reach it.

**Streaming vs buffering an endpoint** (`BufferedTicksVariation`)

| Endpoint | First byte | Server memory | An error halfway |
|---|---|---|---|
| return `IAsyncEnumerable<T>` (`/ticks`, original) | after the **first** item | one item | 200, then the body is cut off |
| `await ...ToListAsync()` then `Ok(list)` (`/variations/ticks/buffered`) | after the **last** item | the whole list | a proper 500 (nothing was sent yet) |

**Other ways to get an async stream** (elsewhere in the repo):

| Source | Where |
|---|---|
| `channel.Reader.ReadAllAsync()` | 5.2, 7.3, 9.6 |
| gRPC server stream `call.ResponseStream.ReadAllAsync()`; manual `MoveNext` loop | 8.9 |
| `IObservable<T>` (Rx) — **pushes** items instead of you **pulling** them | 9.8 |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `IAsyncEnumerable<T>` | items arrive over time and waiting shouldn't block a thread | `Task<List<T>>` waits for everything; `IEnumerable<T>` blocks while waiting |
| `[EnumeratorCancellation]` | `WithCancellation(token)` reaches the iterator | without it the caller's token is ignored |
| `AsAsyncEnumerable()` | rows handled one by one; memory stays flat for big tables | `ToListAsync()` loads every row first |
| `TimeSpan delay` parameter | tests run fast (5 ms), the demo slow (500 ms) | a fixed delay would make tests slow |
| in-memory SQLite + one open connection | no file left behind; each test app has its own database | a file database is shared by parallel tests |
| `HasConversion<double>()` for `Total` | SQLite can't compare or sort `decimal` | — |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `PriceTickGenerator` | ticks in order | 0 → empty; cancel after 2 keeps those 2 | cancelled token throws at the first `MoveNextAsync`, not at the call |
| `OrderStreamService` | rows streamed, summed, async LINQ | nothing big enough → empty / 0 | cancelled token throws |
| `/ticks` | first tick arrives before the last is made | buffered: same JSON, nothing until the end | failure halfway: 200, then body cut off |
| `/orders/big` | rows as a JSON array; sum 1419 | high minimum → `[]` | `min=lots` → 400 |
| cancellation variations | all three give everything | no-token ignores cancel; quiet one stops silently | attribute version throws |
