# Project 5.1 — Multi-Threaded File Downloader

Downloads are **simulated** (sleep, then write random bytes to a file in your temp folder),
so the demo runs offline and gives the same kind of output every time.

```bash
dotnet run
```

## What it covers

| Feature | Where |
|---|---|
| `new Thread(...)`, `Start`, `Join`, `IsBackground` | Part 1 (race demo) and the progress thread |
| `ThreadPool.QueueUserWorkItem` | Part 2a, one work item per file |
| `CountdownEvent` | Wait until all pool work items are done |
| `lock` | Protects the `log` list |
| `Interlocked.Increment/Add/Decrement/CompareExchange` | All counters |
| `SemaphoreSlim` + `Wait()` | At most 3 downloads at once (Part 2) |
| `Mutex` | Named lock that works across separate programs (Part 4) |
| `Semaphore` (no "Slim") | 2 slots shared by 5 threads (Part 5) |
| `SemaphoreSlim` + `WaitAsync()` | At most 2 of 6 async jobs at once (Part 6) |
| `EventHandler<DownloadFinishedEventArgs>` | `DownloadFinished` event |
| `Func`, `Action`, `Predicate` | Part 3 |

## Thread safety rules (the invariants)

1. **Every shared number is changed with `Interlocked`.**
   `count++` is really three steps: read, add 1, write. If two threads read the same value at the
   same time, one update is lost. Part 1 proves it: the plain counter ends up well below 400,000.
   `Interlocked.Increment` does all three steps as one unbreakable step.
2. **Shared numbers are read with `Volatile.Read` / `Interlocked.Read`.** This makes sure the
   progress thread sees the newest value. (`Interlocked.Read` is needed for `long`, because on
   32-bit machines a 64-bit value can be read half-old, half-new.)
3. **The `List<string>` log is only touched inside `lock (logLock)`.** `List<T>` isn't thread-safe:
   two `Add` calls at once can corrupt it. Interlocked only works on single numbers, so for a list
   we need a real lock. `GetLog()` returns a *copy*, so nobody can use the list outside the lock.
4. **The lock object is private.** Nobody outside the class can lock on it, so no outside code can
   cause a deadlock with our lock.
5. **At most 3 downloads run at once.** `SemaphoreSlim(3)` hands out 3 slots. `Release()` is in a
   `finally`, so a slot is always given back even if a download throws — otherwise the
   downloader would slowly lock up.
6. **`MostRunningAtOnce` uses compare-and-swap.** "If it's bigger, replace it" is two steps (compare,
   then write). `CompareExchange` only writes if the value is still what we compared against;
   otherwise it loops and tries again.

## Why these choices

- **ThreadPool instead of 10 new threads:** creating a thread is expensive (about 1 MB of stack
  memory each). The pool reuses a few threads for many short jobs.
- **A dedicated thread for progress:** it runs for the whole download and mostly sleeps — a
  good fit for its own thread. It's a *background* thread so it can never keep the app open.
- **`Semaphore` vs `SemaphoreSlim`:** both are created with two numbers, `(initialCount, maximumCount)`,
  and hand out that many slots. `Semaphore` (Part 5) is the older class; `SemaphoreSlim` is lighter and
  faster, and it also has `WaitAsync()` for async code. Use `SemaphoreSlim` unless you have a reason not to.
- **`Wait()` vs `WaitAsync()`:** the downloader (Part 2) calls `Wait()`, which **blocks a thread** until a
  slot is free - fine here, because those are thread-pool threads doing blocking work anyway. In `async`
  code, use `await WaitAsync()` (Part 6): it waits **without holding a thread**, so thousands of waiting
  jobs cost almost nothing. Example: limiting how many `HttpClient` calls run at once.
- **`Release()` always goes in `finally`** for every semaphore, or one failed job loses a slot forever.
- **`Mutex` only at the end:** it's much slower than `lock`, but it's the only one here that works
  *between programs* (e.g. "only one copy of this app may run"). Use `lock` for anything inside one app.
- **Real code would use `async`/`await`** with `HttpClient` for downloads, because a thread waiting
  for the network is a wasted thread. This project uses threads on purpose to learn the basics.

## Code snippets

The key lines from `Program.cs`.

**Race condition vs `Interlocked`**

```csharp
unsafeCounter++;                                // read, add, write: two threads can overlap and lose updates
Interlocked.Increment(ref interlockedCounter);  // one atomic step: never loses updates
```

**Creating threads and waiting for them**

```csharp
var thread = new Thread(() =>
{
    // work
});
thread.IsBackground = true; // won't keep the app alive on its own
thread.Start();
thread.Join();              // wait for it to finish
```

**`ThreadPool` + `CountdownEvent`: run many jobs, wait for all of them**

```csharp
using var allDone = new CountdownEvent(fileNames.Length);
foreach (string fileName in fileNames)
{
    ThreadPool.QueueUserWorkItem(_ =>
    {
        downloader.Download(fileName);
        allDone.Signal();
    });
}
allDone.Wait();
```

**`SemaphoreSlim`: at most 3 at once, and `lock` around a shared `List`**, from `FileDownloader.Download`

```csharp
downloadSlots.Wait(); // wait here if 3 downloads are already running
try
{
    // ... download ...
    lock (logLock) // List<T> is not thread-safe, so only one thread may add at a time
    {
        log.Add($"{DateTime.Now:HH:mm:ss.fff} {fileName} {data.Length} bytes");
    }
    DownloadFinished?.Invoke(this, new DownloadFinishedEventArgs(fileName, data.Length, Environment.CurrentManagedThreadId));
}
finally
{
    downloadSlots.Release(); // ALWAYS give the slot back, even if the download failed
}
```

**`SemaphoreSlim.WaitAsync`: limit async work without blocking a thread**

```csharp
using var asyncSlots = new SemaphoreSlim(2);

async Task RunLimitedJobAsync(int jobNumber)
{
    await asyncSlots.WaitAsync(); // no thread is blocked while waiting here
    try
    {
        await Task.Delay(150);
    }
    finally
    {
        asyncSlots.Release();
    }
}
```

**`Semaphore` (no "Slim") and `Mutex`**

```csharp
using var twoSlotSemaphore = new Semaphore(initialCount: 2, maximumCount: 2);
twoSlotSemaphore.WaitOne();
twoSlotSemaphore.Release();

using var mutex = new Mutex(false, "Global-Roadmap-ThreadedFileDownloader"); // works between PROGRAMS
if (mutex.WaitOne(TimeSpan.Zero))
{
    Console.WriteLine("No other copy of this program is running.");
    mutex.ReleaseMutex();
}
```

**Events and delegates: `event`, `Func`, `Action`, `Predicate`**

```csharp
public event EventHandler<DownloadFinishedEventArgs>? DownloadFinished;

downloader.DownloadFinished += (sender, eventArgs) =>
{
    Console.WriteLine($"{eventArgs.FileName} done: {eventArgs.SizeInBytes:N0} bytes");
};

Func<long, string> formatSize = bytes => $"{bytes / 1024.0:0.0} KB";
Predicate<string> isEvenFile = name => int.Parse(name.Substring(4, 2)) % 2 == 0;
Action<string> printLine = text => Console.WriteLine("  " + text);
```

## Variations

Other ways to write the same code, in `Variations/` (results below from running them; 3 runs for the counters).

**4 threads × 100,000 additions to one counter** (`SharedCounterVariation`, expected 400,000)

| Way | Run 1 | Run 2 | Run 3 | Safe? |
|---|---|---|---|---|
| `counter++` | 207,024 | 189,590 | 234,431 | ❌ updates lost, different every run |
| `Interlocked.Increment(ref counter)` (original) | 400,000 | 400,000 | 400,000 | ✅ fastest for one number |
| `lock (object) { counter++; }` | 400,000 | 400,000 | 400,000 | ✅ use when **several** values change together |
| `lock (System.Threading.Lock) { ... }` (.NET 9+) | 400,000 | 400,000 | 400,000 | ✅ a dedicated lock type; can't lock on the wrong object by accident |
| `Volatile.Write(ref c, Volatile.Read(ref c) + 1)` | 151,608 | 166,526 | 167,899 | ❌ **volatile is not atomic** — read and write are still two steps |

**12 jobs of ~50 ms, at most 3 at once** (`RunManyJobsVariation`; each returns the highest number running together)

| Way | Most at once | Time | The difference |
|---|---|---|---|
| `ThreadPool` + `CountdownEvent` + `SemaphoreSlim.Wait` (original style) | 3 | 206 ms | a waiting job **blocks a thread** |
| `Task.WhenAll` + `SemaphoreSlim.WaitAsync` | 3 | 215 ms | waits without holding a thread; the usual choice in async code |
| `Parallel.ForEachAsync` with `MaxDegreeOfParallelism = 3` | 3 | 212 ms | the limit is **one option**, no semaphore to write |
| `Parallel.For` | 3 | 216 ms | for CPU work; blocks the caller until done |
| `Task.WhenAll` with **no limit** | **12** | 51 ms | everything at once: fine for 12, floods a server with 10,000 |

**Notifying listeners** (`NotifyListenersVariation`)

| Way | Outside code can | Test result |
|---|---|---|
| `public event Action<string>? FinishedEvent` (original style) | only `+=` / `-=` | calling it or assigning with `=` from outside is a compile error (`CS0079`) |
| `public Action<string>? FinishedDelegate` (a plain field) | `+=`, **call it**, **replace it with `=`** | `= ...` silently removed the first handler; outside code "finished" a fake download |

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| Counters are **fields** (`int finishedCount`), shown through properties | `Interlocked.Increment(ref finishedCount)` needs `ref` to a real variable. | A property is really a method; you can't take `ref` of it. |
| `int` counters, `long totalBytes` | File counts are small; total bytes can pass 2 GB (int max). | `int` bytes would overflow and wrap negative. |
| `Interlocked.Read` for the `long` | On 32-bit machines a 64-bit read can be half old, half new. | `Volatile.Read` is enough for `int` only. |
| `List<string> log` + `lock` | List isn't thread-safe; the lock lets one thread add at a time. | `ConcurrentBag` has no order. `ConcurrentQueue` would also work, but the point here is learning `lock`. |
| `GetLog()` returns `List<string>` copy | Copy made inside the lock; caller can't touch the shared list. | Returning the real list (or a lazy `IEnumerable`) lets callers read it without the lock. |
| `object logLock` | Private, used only for locking. | `lock (this)` lets outside code take the same lock → deadlock risk. |
| `SemaphoreSlim` | "At most 3 at once" is exactly what a semaphore counts. | `lock` allows only 1. `Semaphore` (no Slim) is slower and has no `WaitAsync`. |
| `Semaphore` (Part 5, demo only) | Shows the older class next to `SemaphoreSlim`. | Not used for the real work: `SemaphoreSlim` is faster and can `await`. |
| `await WaitAsync()` (Part 6) | Waits for a slot without blocking a thread. | `Wait()` inside async code wastes a thread per waiting job. |
| `class DownloadFinishedEventArgs : EventArgs` | The .NET convention for `EventHandler<T>`: data in a class with get-only properties. | A tuple or bare parameters don't fit the `EventHandler<T>` pattern. |
| `event EventHandler<DownloadFinishedEventArgs>` | `event` lets outside code subscribe (`+=`) but not fire or clear the event. | A public `Action` field could be overwritten or invoked by anyone. |
| `Func<long, string>` | A function that takes a value and returns one. | — |
| `Action<string>` | A function that returns nothing. | — |
| `Predicate<string>` | `Array.FindAll` asks for a `Predicate<T>` (older API). Same as `Func<T, bool>`. | `Func<string, bool>` doesn't convert to `Predicate` automatically. |
| `string[] fileNames` | 10 names, never changes. | — |
| `Thread` and `ThreadPool` instead of `Task` | The project is *about* the low-level pieces. | Real code would use `Task` + `async` (see 5.2). |
| `Mutex` (once) | The only lock here that works between separate programs. | Far slower than `lock`; never used for in-app locking. |
