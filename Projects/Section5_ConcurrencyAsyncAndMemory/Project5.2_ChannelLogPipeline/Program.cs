// This whole app is ONE file. The program runs from the top.
// The classes it uses (LogTotals, LogLineParser, SettingCache) are defined at the BOTTOM of this file.
// (In C#, top-level code must come before any class definitions in the file.)

using System.Buffers;
using System.Diagnostics;
using System.Threading.Channels;

// Pipeline:  producer --(bounded channel, 100 lines)--> 3 consumers --> totals
// Stops after 3 seconds, or when you press Ctrl+C.

using var stopSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
// A browser (the Playground) has no Ctrl+C, and Console.CancelKeyPress throws there - so only hook it elsewhere.
if (!OperatingSystem.IsBrowser())
{
    Console.CancelKeyPress += (sender, eventArgs) =>
    {
        eventArgs.Cancel = true; // don't kill the app, let it shut down cleanly
        stopSource.Cancel();
    };
}

// Bounded = at most 100 lines waiting. When full, the producer WAITS (backpressure).
Channel<string> logLines = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleWriter = true,
});

var totals = new LogTotals();
long producedCount = 0;
long timesProducerHadToWait = 0;

Task producer = ProduceLogLinesAsync(stopSource.Token);
Task[] consumers = new Task[3];
for (int i = 0; i < consumers.Length; i++)
{
    consumers[i] = ConsumeLogLinesAsync();
}

await producer;
await Task.WhenAll(consumers);

Console.WriteLine($"Produced: {producedCount:N0} lines, producer had to wait (channel full) {timesProducerHadToWait:N0} times");
Console.WriteLine($"Consumed: {totals.Info + totals.Warning + totals.Error:N0} lines");
Console.WriteLine($"  INFO {totals.Info:N0}   WARN {totals.Warning:N0}   ERROR {totals.Error:N0}");
Console.WriteLine($"  Slowest request: {totals.SlowestMilliseconds} ms");
Console.WriteLine();

CompareStringSplitWithSpan();
await ShowValueTaskAndArrayPoolAsync();
return;

async Task ProduceLogLinesAsync(CancellationToken stopToken)
{
    string[] levels = ["INFO", "INFO", "INFO", "WARN", "ERROR"];
    try
    {
        while (!stopToken.IsCancellationRequested)
        {
            string level = levels[Random.Shared.Next(levels.Length)];
            string line = $"{DateTime.UtcNow:O} {level} {Random.Shared.Next(1, 2000)} /api/orders";

            // TryWrite fails if the channel is full; then WriteAsync waits until there's room.
            if (!logLines.Writer.TryWrite(line))
            {
                timesProducerHadToWait++;
                await logLines.Writer.WriteAsync(line, stopToken);
            }
            producedCount++;
        }
    }
    catch (OperationCanceledException)
    {
        // Normal way to stop.
    }
    finally
    {
        logLines.Writer.Complete(); // tells consumers "no more lines are coming"
    }
}

async Task ConsumeLogLinesAsync()
{
    // ReadAllAsync ends when the writer calls Complete() and the channel is empty.
    // No cancellation token here on purpose: consumers finish the lines already queued (no data lost).
    await foreach (string line in logLines.Reader.ReadAllAsync())
    {
        LogLineParser.ParseAndCount(line, totals);
        await Task.Yield(); // pretend we did some async work (e.g. writing to a database)
    }
}

void CompareStringSplitWithSpan()
{
    string line = "2026-11-05T10:00:00.0000000Z ERROR 1234 /api/orders";
    const int repeat = 1_000_000;

    long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
    var timer = Stopwatch.StartNew();
    for (int i = 0; i < repeat; i++)
    {
        string[] parts = line.Split(' '); // creates 1 array + 4 new strings every time
        int.Parse(parts[2]);
    }
    long splitBytes = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
    long splitTime = timer.ElapsedMilliseconds;

    bytesBefore = GC.GetAllocatedBytesForCurrentThread();
    timer.Restart();
    for (int i = 0; i < repeat; i++)
    {
        LogLineParser.ReadDurationWithSpan(line); // only looks at the existing string, creates nothing
    }
    long spanBytes = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
    long spanTime = timer.ElapsedMilliseconds;

    Console.WriteLine($"Parse {repeat:N0} lines:");
    Console.WriteLine($"  string.Split : {splitTime,5} ms, {splitBytes / 1024 / 1024,5} MB allocated");
    Console.WriteLine($"  Span         : {spanTime,5} ms, {spanBytes / 1024 / 1024,5} MB allocated");
}

async Task ShowValueTaskAndArrayPoolAsync()
{
    var cache = new SettingCache();
    string first = await cache.GetSettingAsync("timeout");  // slow path: real async work
    string second = await cache.GetSettingAsync("timeout"); // fast path: no Task object created
    Console.WriteLine();
    Console.WriteLine($"ValueTask: first={first}, second={second} (second came from the cache)");

    // ArrayPool: borrow a buffer instead of creating a new one every time.
    byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
    try
    {
        Span<byte> usablePart = buffer.AsSpan(0, 4096); // Rent may give a bigger array; only use what we asked for
        usablePart.Fill(1);
        Console.WriteLine($"Borrowed a {buffer.Length}-byte buffer from ArrayPool, sum of first 4096 = {Sum(usablePart)}");
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer); // always give it back
    }

    // Memory<T> is like Span<T> but can be stored in a field or used across "await".
    Memory<byte> memory = new byte[10];
    await FillLaterAsync(memory);
    Console.WriteLine($"Memory<T> filled after await: {string.Join(",", memory.ToArray())}");
}

static int Sum(ReadOnlySpan<byte> values)
{
    int total = 0;
    foreach (byte value in values)
    {
        total += value;
    }
    return total;
}

static async Task FillLaterAsync(Memory<byte> memory)
{
    await Task.Delay(10).ConfigureAwait(false); // library-style code: no need to come back to the original context
    memory.Span.Fill(7);
}

public class LogTotals
{
    public long Info;
    public long Warning;
    public long Error;
    public int SlowestMilliseconds;
}

public static class LogLineParser
{
    // Line format: "<timestamp> <LEVEL> <milliseconds> <path>"
    public static void ParseAndCount(string line, LogTotals totals)
    {
        ReadOnlySpan<char> rest = line.AsSpan();
        ReadOnlySpan<char> level = NextWord(ref rest, skipFirst: true);
        int milliseconds = int.Parse(NextWord(ref rest, skipFirst: false));

        if (level.SequenceEqual("ERROR"))
        {
            Interlocked.Increment(ref totals.Error);
        }
        else if (level.SequenceEqual("WARN"))
        {
            Interlocked.Increment(ref totals.Warning);
        }
        else
        {
            Interlocked.Increment(ref totals.Info);
        }

        int currentSlowest = Volatile.Read(ref totals.SlowestMilliseconds);
        while (milliseconds > currentSlowest)
        {
            int valueBeforeSwap = Interlocked.CompareExchange(ref totals.SlowestMilliseconds, milliseconds, currentSlowest);
            if (valueBeforeSwap == currentSlowest)
            {
                break;
            }
            currentSlowest = valueBeforeSwap;
        }
    }

    public static int ReadDurationWithSpan(string line)
    {
        ReadOnlySpan<char> rest = line.AsSpan();
        NextWord(ref rest, skipFirst: true);
        return int.Parse(NextWord(ref rest, skipFirst: false));
    }

    // Returns the next space-separated word and moves "rest" past it. No new strings are created.
    private static ReadOnlySpan<char> NextWord(ref ReadOnlySpan<char> rest, bool skipFirst)
    {
        if (skipFirst)
        {
            rest = rest.Slice(rest.IndexOf(' ') + 1);
        }
        int space = rest.IndexOf(' ');
        ReadOnlySpan<char> word = space < 0 ? rest : rest.Slice(0, space);
        rest = space < 0 ? ReadOnlySpan<char>.Empty : rest.Slice(space + 1);
        return word;
    }
}

public class SettingCache
{
    private readonly Dictionary<string, string> cachedValues = new();

    // ValueTask: when the answer is already cached we return it with NO Task allocation.
    public ValueTask<string> GetSettingAsync(string name)
    {
        if (cachedValues.TryGetValue(name, out string? cachedValue))
        {
            return ValueTask.FromResult(cachedValue);
        }
        return new ValueTask<string>(LoadSlowlyAsync(name));
    }

    private async Task<string> LoadSlowlyAsync(string name)
    {
        await Task.Delay(50);
        string value = name + "=30s";
        cachedValues[name] = value;
        return value;
    }
}
