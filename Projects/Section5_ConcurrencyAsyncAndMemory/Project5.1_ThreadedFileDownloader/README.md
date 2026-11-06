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
| `SemaphoreSlim` | At most 3 downloads at once |
| `Mutex` | Named lock that works across separate programs |
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
- **`SemaphoreSlim` instead of `Semaphore`:** the "Slim" version is faster and works inside one
  program, which is all we need. It also supports `WaitAsync` for async code (used in 5.2-style code).
- **`Mutex` only at the end:** it's much slower than `lock`, but it's the only one here that works
  *between programs* (e.g. "only one copy of this app may run"). Use `lock` for anything inside one app.
- **Real code would use `async`/`await`** with `HttpClient` for downloads, because a thread waiting
  for the network is a wasted thread. This project uses threads on purpose to learn the basics.

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
| `SemaphoreSlim` | "At most 3 at once" is exactly what a semaphore counts. | `lock` allows only 1. `Semaphore` (no Slim) is slower and cross-process. |
| `class DownloadFinishedEventArgs : EventArgs` | The .NET convention for `EventHandler<T>`: data in a class with get-only properties. | A tuple or bare parameters don't fit the `EventHandler<T>` pattern. |
| `event EventHandler<DownloadFinishedEventArgs>` | `event` lets outside code subscribe (`+=`) but not fire or clear the event. | A public `Action` field could be overwritten or invoked by anyone. |
| `Func<long, string>` | A function that takes a value and returns one. | — |
| `Action<string>` | A function that returns nothing. | — |
| `Predicate<string>` | `Array.FindAll` asks for a `Predicate<T>` (older API). Same as `Func<T, bool>`. | `Func<string, bool>` doesn't convert to `Predicate` automatically. |
| `string[] fileNames` | 10 names, never changes. | — |
| `Thread` and `ThreadPool` instead of `Task` | The project is *about* the low-level pieces. | Real code would use `Task` + `async` (see 5.2). |
| `Mutex` (once) | The only lock here that works between separate programs. | Far slower than `lock`; never used for in-app locking. |
