// Run 12 small jobs (each ~50 ms of waiting), at most 3 at the same time, and wait for all of them.
// Every version does the same work; they differ in threads used, readability and async-friendliness.
// Each returns the highest number of jobs that were running at once.
public static class RunManyJobsVariation
{
    private const int JobCount = 12;
    private const int Limit = 3;

    // ORIGINAL style: ThreadPool + CountdownEvent + SemaphoreSlim.Wait (BLOCKS a thread while waiting for a slot).
    public static int WithThreadPool()
    {
        var tracker = new ConcurrencyTracker();
        using var slots = new SemaphoreSlim(Limit);
        using var allDone = new CountdownEvent(JobCount);
        for (int i = 0; i < JobCount; i++)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                slots.Wait();
                try
                {
                    tracker.Run(() => Thread.Sleep(50));
                }
                finally
                {
                    slots.Release();
                    allDone.Signal();
                }
            });
        }
        allDone.Wait();
        return tracker.Highest;
    }

    // Task.WhenAll + SemaphoreSlim.WaitAsync: no thread is blocked while waiting. The normal choice in async code.
    public static async Task<int> WithWhenAll()
    {
        var tracker = new ConcurrencyTracker();
        using var slots = new SemaphoreSlim(Limit);
        var jobs = Enumerable.Range(0, JobCount).Select(async _ =>
        {
            await slots.WaitAsync();
            try
            {
                await tracker.RunAsync(() => Task.Delay(50));
            }
            finally
            {
                slots.Release();
            }
        });
        await Task.WhenAll(jobs);
        return tracker.Highest;
    }

    // Parallel.ForEachAsync: the limit is ONE option - no semaphore to write. Shortest version.
    public static async Task<int> WithParallelForEachAsync()
    {
        var tracker = new ConcurrencyTracker();
        await Parallel.ForEachAsync(Enumerable.Range(0, JobCount), new ParallelOptions { MaxDegreeOfParallelism = Limit }, async (_, cancellationToken) =>
        {
            await tracker.RunAsync(() => Task.Delay(50, cancellationToken));
        });
        return tracker.Highest;
    }

    // Parallel.For: for CPU work (no awaiting inside). Blocks the calling thread until all are done.
    public static int WithParallelFor()
    {
        var tracker = new ConcurrencyTracker();
        Parallel.For(0, JobCount, new ParallelOptions { MaxDegreeOfParallelism = Limit }, _ => tracker.Run(() => Thread.Sleep(50)));
        return tracker.Highest;
    }

    // Task.WhenAll with NO limit: all 12 start at once. Fine for 12; for 10,000 downloads it floods the network.
    public static async Task<int> WithWhenAllNoLimit()
    {
        var tracker = new ConcurrencyTracker();
        await Task.WhenAll(Enumerable.Range(0, JobCount).Select(_ => tracker.RunAsync(() => Task.Delay(50))));
        return tracker.Highest;
    }

    // Counts how many jobs are inside Run at the same moment, and remembers the highest.
    private sealed class ConcurrencyTracker
    {
        private int now;
        private int highest;

        public int Highest => Volatile.Read(ref highest);

        public void Run(Action work)
        {
            Enter();
            try
            {
                work();
            }
            finally
            {
                Interlocked.Decrement(ref now);
            }
        }

        public async Task RunAsync(Func<Task> work)
        {
            Enter();
            try
            {
                await work();
            }
            finally
            {
                Interlocked.Decrement(ref now);
            }
        }

        private void Enter()
        {
            int current = Interlocked.Increment(ref now);
            int seen = Volatile.Read(ref highest);
            while (current > seen && Interlocked.CompareExchange(ref highest, current, seen) != seen)
            {
                seen = Volatile.Read(ref highest);
            }
        }
    }
}
