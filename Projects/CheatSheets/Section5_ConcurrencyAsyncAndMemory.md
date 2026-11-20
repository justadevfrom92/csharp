# Cheat Sheet — Section 5: Concurrency, Async & Memory

Projects: [5.1](../Section5_ConcurrencyAsyncAndMemory/Project5.1_ThreadedFileDownloader), [5.2](../Section5_ConcurrencyAsyncAndMemory/Project5.2_ChannelLogPipeline), [5.3](../Section5_ConcurrencyAsyncAndMemory/Project5.3_Benchmarks), [5.4](../Section5_ConcurrencyAsyncAndMemory/Project5.4_AsyncStreams)

## async / await

```csharp
public async Task<string> LoadAsync(string path)
{
    string text = await File.ReadAllTextAsync(path); // the thread is free while waiting
    return text.ToUpperInvariant();
}

public async Task SaveAsync(string path, string text)   // no result: Task
{
    await File.WriteAllTextAsync(path, text);
}
```

Rules: `async` all the way up; never `.Result` or `.Wait()` (deadlocks); `async void` only for event handlers.

## Run several at once

```csharp
Task<string> first = LoadAsync("a.txt");
Task<string> second = LoadAsync("b.txt");
string[] both = await Task.WhenAll(first, second);   // wait for all
Task<string> winner = await Task.WhenAny(first, second); // first to finish

await Parallel.ForEachAsync(urls, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (url, token) =>
{
    await DownloadAsync(url, token);
});
```

## Cancellation and timeouts

```csharp
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await DownloadAsync(url, cancellation.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Took too long, stopped.");
}

public async Task DownloadAsync(string url, CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    await Task.Delay(1000, cancellationToken);           // pass the token on
}

string result = await LoadAsync("a.txt").WaitAsync(TimeSpan.FromSeconds(2));
```

## Threads (rarely needed directly)

```csharp
var thread = new Thread(() => DoWork());
thread.IsBackground = true;
thread.Start();
thread.Join();

ThreadPool.QueueUserWorkItem(_ => DoWork());
await Task.Run(() => HeavyCalculation());   // CPU work off the current thread
```

## Protecting shared data

```csharp
private readonly object listLock = new();
lock (listLock)                              // one thread at a time
{
    items.Add(item);
}

Interlocked.Increment(ref counter);          // one number, atomically
Interlocked.Add(ref total, amount);
int seen = Volatile.Read(ref counter);

var cache = new ConcurrentDictionary<string, int>();
int value = cache.GetOrAdd("key", key => key.Length);

private readonly SemaphoreSlim slots = new(3); // at most 3 at once
await slots.WaitAsync();                       // async-friendly (lock can't await)
try
{
    await DoWorkAsync();
}
finally
{
    slots.Release();
}
```

## Channels (producer / consumer)

```csharp
Channel<string> channel = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait, // producer waits when full
});

await channel.Writer.WriteAsync("job");   // producer
channel.Writer.Complete();                 // "no more"

await foreach (string job in channel.Reader.ReadAllAsync()) // consumer
{
}
```

## Async streams (`IAsyncEnumerable`) — 5.4

```csharp
public static async IAsyncEnumerable<int> TicksAsync(int count, [EnumeratorCancellation] CancellationToken token = default)
{
    for (int number = 1; number <= count; number++)
    {
        await Task.Delay(100, token);
        yield return number;
    }
}

await foreach (int tick in TicksAsync(5).WithCancellation(token))
{
}

List<int> firstTwo = await TicksAsync(5).Where(tick => tick > 1).Take(2).ToListAsync(); // .NET 10: built in
IAsyncEnumerable<OrderModel> rows = database.Orders.AsAsyncEnumerable();                  // EF Core, row by row
app.MapGet("/ticks", () => TicksAsync(5));                                                // streamed JSON
```

Forget `[EnumeratorCancellation]` → `WithCancellation(token)` is ignored. A streamed endpoint can't return 500 halfway: check first.

## Events and delegates

```csharp
Func<int, int, int> add = (a, b) => a + b;       // takes inputs, returns a value
Action<string> log = text => Console.WriteLine(text); // returns nothing
Predicate<int> isEven = number => number % 2 == 0;    // returns bool

public event EventHandler<OrderPlacedEventArgs>? OrderPlaced;
OrderPlaced?.Invoke(this, new OrderPlacedEventArgs(orderId)); // raise
service.OrderPlaced += (sender, args) => Console.WriteLine(args.OrderId); // subscribe
```

## ValueTask

```csharp
public ValueTask<string> GetAsync(string key)
{
    if (cache.TryGetValue(key, out string? value))
    {
        return ValueTask.FromResult(value);              // no allocation on the fast path
    }
    return new ValueTask<string>(LoadSlowAsync(key));
}
```

## Memory: Span, ArrayPool, using

```csharp
ReadOnlySpan<char> line = "2026-11-05 ERROR 120".AsSpan();
ReadOnlySpan<char> level = line.Slice(11, 5);          // no new string
int milliseconds = int.Parse(line[17..]);

byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

using var file = File.OpenRead("data.bin");            // Dispose() at the end of the scope
await using var stream = File.Create("out.bin");       // DisposeAsync()

long allocated = GC.GetAllocatedBytesForCurrentThread();
```

## Measuring

```csharp
var timer = Stopwatch.StartNew();
DoWork();
Console.WriteLine($"{timer.ElapsedMilliseconds} ms");
```

```csharp
[MemoryDiagnoser]
public class ParseBenchmark
{
    [Params(10, 1000)]
    public int Size { get; set; }

    [Benchmark(Baseline = true)]
    public int WithSplit()
    {
        return "a b c".Split(' ').Length;
    }
}
// Program.cs: BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
```

```bash
dotnet run -c Release -- --filter "*Parse*"
```
