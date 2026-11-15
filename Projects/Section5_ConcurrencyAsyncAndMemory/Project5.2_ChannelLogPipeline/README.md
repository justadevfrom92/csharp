# Project 5.2 — Async Producer/Consumer Log Pipeline

```bash
dotnet run -c Release
```
Runs for 3 seconds (or until Ctrl+C), then prints totals and a Split-vs-Span comparison.

## The pipeline

```
 producer ──► [ bounded channel: max 100 lines ] ──► consumer 1 ─┐
 (1 task)                                       ├──► consumer 2 ─┼──► LogTotals (Interlocked)
                                                └──► consumer 3 ─┘
```

## What it covers

- `Channel.CreateBounded` with `BoundedChannelFullMode.Wait` (backpressure)
- `async`/`await`, `Task`, `Task.WhenAll`, `await foreach` + `ReadAllAsync`
- `CancellationTokenSource` with a timeout + Ctrl+C for clean shutdown
- `ReadOnlySpan<char>` parsing with no allocations, measured against `string.Split`
- `ValueTask<T>` (cache hit = no allocation), `ArrayPool<T>`, `Memory<T>`, `ConfigureAwait(false)`

## How backpressure works, and why

**The problem:** the producer can make lines far faster than consumers can handle them. With an
*unbounded* channel the waiting lines pile up in memory forever, until the app runs out of memory.

**The fix:** a *bounded* channel holds at most 100 lines. When it's full, `WriteAsync` simply
*waits* (without blocking a thread) until a consumer takes a line. So the producer is
automatically slowed down to the speed of the consumers. Memory use stays flat no matter how long
it runs. The output shows how many times the producer had to wait.

**Why `Wait` and not `DropOldest`/`DropNewest`:** these are log lines we want to count, so losing
any is wrong. Drop modes are fine for things like live sensor readings where only the latest matters.

**Why 100:** big enough to smooth out short bursts, small enough that memory stays tiny.

**Why 3 consumers and 1 producer:** parsing is the slow part, so we put more workers there.
`SingleWriter = true` lets the channel use a faster internal path.

## Clean shutdown

1. The token is cancelled (3 s timeout or Ctrl+C — `eventArgs.Cancel = true` stops .NET from killing the app).
2. The producer stops, and in `finally` calls `Writer.Complete()` — "nothing more is coming".
3. Consumers are **not** given the token. They keep reading until the channel is empty, then
   `ReadAllAsync` ends by itself. So every produced line is counted: *Produced == Consumed*.

## Why Span

`line.Split(' ')` creates a new array and a new string for every word — 236 MB of garbage for a
million lines, which the garbage collector then has to clean up. A `ReadOnlySpan<char>` is just
a "window" onto the existing string: slicing it creates nothing. Same result, ~5x faster, 0 MB.
Spans can't be used across `await` (they live on the stack), which is why `Memory<T>` exists.

## Code snippets

The key lines from `Program.cs`.

**A bounded `Channel`: the producer waits when 100 lines are queued (backpressure)**

```csharp
Channel<string> logLines = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleWriter = true,
});
```

**Producer: write, then `Complete()` so consumers know to stop**

```csharp
try
{
    while (!stopToken.IsCancellationRequested)
    {
        await logLines.Writer.WriteAsync(line, stopToken);
    }
}
catch (OperationCanceledException)
{
    // Normal way to stop.
}
finally
{
    logLines.Writer.Complete(); // "no more lines are coming"
}
```

**Consumers: `await foreach` until the channel is empty and complete**

```csharp
await foreach (string line in logLines.Reader.ReadAllAsync())
{
    LogLineParser.ParseAndCount(line, totals);
}

Task[] consumers = new Task[3];
for (int i = 0; i < consumers.Length; i++)
{
    consumers[i] = ConsumeLogLinesAsync();
}
await Task.WhenAll(consumers);
```

**Stopping cleanly: `CancellationTokenSource` with a timeout and Ctrl+C**

```csharp
using var stopSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true; // don't kill the app, let it shut down cleanly
    stopSource.Cancel();
};
```

**`Span<T>`: read part of a string without creating new strings**

```csharp
ReadOnlySpan<char> rest = line.AsSpan();
int space = rest.IndexOf(' ');
ReadOnlySpan<char> word = space < 0 ? rest : rest.Slice(0, space);
if (word.SequenceEqual("ERROR")) { ... }
int milliseconds = int.Parse(word); // int.Parse accepts a span directly
```

**`ValueTask`: no allocation when the answer is already cached**

```csharp
public ValueTask<string> GetSettingAsync(string name)
{
    if (cachedValues.TryGetValue(name, out string? cachedValue))
    {
        return ValueTask.FromResult(cachedValue);
    }
    return new ValueTask<string>(LoadSlowlyAsync(name));
}
```

**`ArrayPool`: borrow a buffer instead of creating one**

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    Span<byte> usablePart = buffer.AsSpan(0, 4096); // Rent may give a bigger array
    usablePart.Fill(1);
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer); // always give it back
}
```

**`Memory<T>` across an `await`, and `ConfigureAwait(false)`**

```csharp
static async Task FillLaterAsync(Memory<byte> memory)
{
    await Task.Delay(10).ConfigureAwait(false); // library-style code
    memory.Span.Fill(7);                        // a Span can't live across await; Memory can
}
```

## Variations

Other ways to write the same code, in `Variations/` (results below from running them).

**A full bounded channel** (`ChannelFullModeVariation`: capacity 3, write 1..10 with nobody reading, then read)

| `FullMode` | Left in the channel | What happened to the rest |
|---|---|---|
| `Wait` (original) | 1, 2, 3 | `TryWrite` returned `false`; `WriteAsync` would **wait** (backpressure — nothing lost) |
| `DropOldest` | 8, 9, 10 | oldest thrown away to make room — "latest values matter" (sensor readings) |
| `DropNewest` | 1, 2, 10 | the newest item **already queued** is replaced each time |
| `DropWrite` | 1, 2, 3 | new items silently ignored |
| `Channel.CreateUnbounded` | 1..10 | nothing dropped, nothing waits — memory grows if the reader is slow |

**Producer/consumer queue** (`ProducerConsumerVariation`, 1,000 items; all three summed to 500,500)

| Queue | How the consumer waits | The difference |
|---|---|---|
| `Channel<T>` (original) | `await foreach ... ReadAllAsync()` | no thread blocked; bounded = backpressure |
| `BlockingCollection<T>` | `GetConsumingEnumerable()` **blocks a thread** | older API; fine on a dedicated thread, wasteful in async code |
| `ConcurrentQueue<T>` | **polls** (`TryDequeue` + `Thread.Yield` in a loop) | thread-safe but no waiting and no size limit |

**Reading the 3rd word of a log line** (`LogLineParseVariation`, 100,000 lines, all returned 1234)

| Way | Memory allocated per line |
|---|---|
| `line.Split(' ')[2]` | 232 bytes (an array + a string per word) |
| `ReadOnlySpan<char>` slicing (original idea) | **0 bytes** |
| `IndexOf` + `Substring` | 32 bytes (one string, for the number) |
| `[GeneratedRegex]` | 384 bytes (a `Match` per line) — easiest to change |

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `Channel<string>` (bounded) | Thread-safe async queue with built-in waiting when full (backpressure). | `Queue<T>` isn't thread-safe; `ConcurrentQueue` can't make the producer wait. |
| `ReadOnlySpan<char>` for parsing | A window onto the existing string — slicing creates nothing (0 MB vs 236 MB). | `string.Split` allocates an array and a string per word. |
| `ref ReadOnlySpan<char> rest` | `NextWord` moves the window forward for the caller. | Returning a tuple would also work; `ref` keeps the call short. |
| `Span<byte>` for the rented buffer | Writable window onto only the part of the array we asked for. | `ArrayPool` may hand back a bigger array; using it directly risks reading leftover bytes. |
| `Memory<byte>` in `FillLaterAsync` | Can survive an `await`. | A `Span` lives on the stack and isn't allowed across `await`. |
| `ArrayPool<byte>` | Borrow and return a buffer instead of allocating each time. | `new byte[4096]` in a hot loop creates garbage. |
| `ValueTask<string>` in `SettingCache` | Most calls are cache hits that finish instantly — no `Task` object allocated. | `Task<string>` allocates even for instant answers. (Rule: await a `ValueTask` only once.) |
| `class LogTotals` with public **fields** | Shared counters updated with `Interlocked.Increment(ref totals.Error)` — needs real fields. | Properties can't be passed by `ref`. Normally we'd use properties; this is the exception. |
| `long` counters | Millions of lines per second; a long run could pass 2.1 billion. | `int` would overflow and wrap. |
| `static class LogLineParser` | Pure parsing functions, no state. | — |
| `Dictionary` in `SettingCache` (not thread-safe) | In this demo only one task uses it. | Shared across threads it would need a lock or `ConcurrentDictionary`. |
| `static` local functions (`Sum`, `FillLaterAsync`) | `static` stops them from accidentally using outside variables. | — |
| `Task[] consumers` | Exactly 3, fixed. | — |
| `CancellationTokenSource(TimeSpan)` | One object handles both the 3-second timeout and Ctrl+C. | — |
