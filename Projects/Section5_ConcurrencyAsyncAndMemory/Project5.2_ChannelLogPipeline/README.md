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
