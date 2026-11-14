# Project 9.8 — Reactive Extensions (Rx / `IObservable<T>`)

Watch a live stream of stock prices: report big jumps, keep a moving average, ignore bursts until prices
settle, and count ticks per second — all with LINQ-style operators on **events over time**.

```bash
cd ReactiveExtensions
dotnet run
```

```bash
cd ReactiveExtensions.Tests
dotnet test
```

## Pull vs push

| | `IEnumerable<T>` | `IObservable<T>` |
|---|---|---|
| Who decides when the next item comes | **you** pull it (`foreach`, `MoveNext()`) | **the source** pushes it (`OnNext`) whenever it happens |
| Good for | data that already exists (a list, a query) | things that happen over time: prices, clicks, sensor readings, messages |
| End / failure | loop ends / exception | `OnCompleted()` / `OnError(exception)` |
| Operators | LINQ | **the same LINQ** + time operators (`Throttle`, `Buffer` by time, `Sample`, `Timeout`) |

`IObservable<T>` and `IObserver<T>` are **built into .NET**. The **Rx** library (`System.Reactive`) adds the
operators. Nothing runs until something calls `Subscribe`; `Subscribe` returns an `IDisposable` — dispose it
to stop listening (that's how you avoid leaks).

## What's in it

| Operator | What it does here | Method |
|---|---|---|
| `GroupBy` + `SelectMany` | one sub-stream per stock symbol | `WatchForJumps` |
| `Buffer(2, 1)` | sliding pairs `[t1,t2], [t2,t3]...` to compare each tick with the one before | `WatchForJumps` |
| `Buffer(3, 1)` + `Average` | moving average of the last 3 prices | `MovingAverage` |
| `Throttle(100 ms)` | wait until the stream is quiet for 100 ms, then pass on the **last** tick (a burst becomes one update) | `SettledPrices` |
| `Buffer(1 s)` | how many ticks arrived in each second | `CountPerWindow` |
| `Observable.Create`, `Subject<T>`, `Observable.Interval`, `Timer`, `Concat`, `ToObservable` | making streams | `Program.cs`, tests |

## Testing time without waiting: `TestScheduler`

Every time-based method takes an `IScheduler`. The app passes `Scheduler.Default` (the real clock); tests pass
a **`TestScheduler`** — a fake clock moved forward by hand (`scheduler.AdvanceBy(...)`). A test about "100 ms
of quiet" or "ticks per second" then runs instantly and gives the **same result every time**, instead of
depending on how busy the machine is. (Same idea as `TimeProvider` in 8.2.)

## Rx vs the other "events" in this repo

| | Use it for |
|---|---|
| C# `event` (5.1) | a class telling its listeners "this happened"; no operators |
| Observer pattern (9.4) | the same, written by hand with an interface |
| `Channel<T>` (5.2) / Dataflow (9.6) | work queues: each item processed **once**, with backpressure |
| SignalR (8.8) | pushing events to **browsers** |
| **Rx (this)** | combining, filtering and timing **streams of events** in one process |

## Code snippets

**Comparing each tick with the previous one, per symbol**, from `Monitors/PriceMonitor.cs`

```csharp
return ticks
    .GroupBy(tick => tick.Symbol)
    .SelectMany(symbolTicks => symbolTicks
        .Buffer(2, 1)
        .Where(pair => pair.Count == 2)
        .Select(pair => new PriceJumpModel(pair[0].Symbol, pair[0].Price, pair[1].Price,
            Math.Round((pair[1].Price - pair[0].Price) / pair[0].Price * 100m, 2))))
    .Where(jump => Math.Abs(jump.ChangePercent) >= percent);
```

**Time operators take a scheduler**

```csharp
return ticks.Throttle(quiet, scheduler);
return ticks.Buffer(window, scheduler).Select(batch => batch.Count);
```

**Making and subscribing to streams**, from `Program.cs`

```csharp
IObservable<int> countdown = Observable.Create<int>(observer =>
{
    observer.OnNext(3);
    observer.OnNext(2);
    observer.OnCompleted();
    return Disposable.Empty;
});

using var ticks = new Subject<PriceTickModel>();           // push into it by hand
using IDisposable subscription = PriceMonitor.WatchForJumps(ticks, 5m)
    .Subscribe(jump => Console.WriteLine($"JUMP {jump.Symbol}"));
ticks.OnNext(new PriceTickModel("MSFT", 400m));

IList<PriceTickModel> settled = await PriceMonitor.SettledPrices(bursts, TimeSpan.FromMilliseconds(100), Scheduler.Default).ToList();
```

**Testing with a fake clock**, from `ReactiveExtensions.Tests/PriceMonitorTest.cs`

```csharp
var scheduler = new TestScheduler();
ITestableObservable<PriceTickModel> ticks = scheduler.CreateHotObservable(
    ReactiveTest.OnNext(TimeSpan.FromMilliseconds(100).Ticks, Tick("X", 1m)),
    ReactiveTest.OnNext(TimeSpan.FromMilliseconds(1500).Ticks, Tick("X", 3m)));
using IDisposable subscription = PriceMonitor.CountPerWindow(ticks, TimeSpan.FromSeconds(1), scheduler).Subscribe(counts.Add);

scheduler.AdvanceTo(TimeSpan.FromSeconds(3).Ticks);   // 3 "seconds" pass instantly
```

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| Methods take and return `IObservable<T>` | Streams compose: the output of one is the input of the next; nothing runs until subscribed. | Returning a `List` would need all the data first. |
| `IScheduler` parameter | Real clock in the app, fake clock in tests. | Hard-coding time makes tests slow and flaky. |
| `Subject<T>` in the demo and tests | An observable you can push into by hand. | In real code, prefer `Observable.Create` or `FromEventPattern`; a `Subject` is easy to misuse as a global event bus. |
| `IDisposable` subscriptions with `using` | Unsubscribe is guaranteed. | A forgotten subscription keeps objects alive forever. |
| `decimal` prices | Money. | `double` (see [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md)). |
| `static class PriceMonitor` | Only functions from stream to stream; no state. | — |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `WatchForJumps` | ±5% moves reported per symbol | 4.99% move, single tick → nothing | `null` stream; percent 0; a source error reaches `OnError` |
| `MovingAverage` | sliding averages | fewer ticks than the window → nothing | `null` stream; window 0 |
| `SettledPrices` (Throttle) | burst of 3 → only the last, exactly 100 ms after it | never quiet long enough → nothing | `null` stream / scheduler |
| `CountPerWindow` | counts per second `[2, 1, 0]` | unsubscribed → stops receiving | `null` stream / scheduler |
