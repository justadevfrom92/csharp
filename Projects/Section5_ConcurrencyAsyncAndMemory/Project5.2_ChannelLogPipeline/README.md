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
