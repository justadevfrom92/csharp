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
