namespace EnvironmentAwareEmail.Variations;

// "Run a job every 100 ms" when the job itself takes 250 ms. Three timers; do runs OVERLAP?
// Each method runs for ~1 second and returns (runs started, most running at the same time).
public static class TimerStyleVariation
{
    // ORIGINAL style (DailyDigestBackgroundService): PeriodicTimer. The next tick is only awaited AFTER the job
    // finishes, so runs NEVER overlap. Ticks that pass while the job runs are skipped.
    public static async Task<(int Runs, int MostAtOnce)> WithPeriodicTimer()
    {
        var tracker = new Tracker();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellation.Token))
            {
                await tracker.JobAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
        return (tracker.Runs, tracker.MostAtOnce);
    }

    // System.Threading.Timer: calls the callback every 100 ms ON A THREAD-POOL THREAD, whether or not the last
    // run has finished. A slow job means SEVERAL copies run at once (two digests sent, two database writes...).
    public static async Task<(int Runs, int MostAtOnce)> WithThreadingTimer()
    {
        var tracker = new Tracker();
        using (var timer = new Timer(_ => tracker.JobAsync().Wait(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100)))
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        await Task.Delay(300); // let running callbacks finish
        return (tracker.Runs, tracker.MostAtOnce);
    }

    // Task.Delay in a loop: no overlap, but the gap is AFTER the job, so each round takes job + delay (350 ms here)
    // and the schedule DRIFTS. Fine for "roughly every so often".
    public static async Task<(int Runs, int MostAtOnce)> WithDelayLoop()
    {
        var tracker = new Tracker();
        var stopAt = DateTime.UtcNow.AddSeconds(1);
        while (DateTime.UtcNow < stopAt)
        {
            await tracker.JobAsync();
            await Task.Delay(100);
        }
        return (tracker.Runs, tracker.MostAtOnce);
    }

    private sealed class Tracker
    {
        private int runs;
        private int now;
        private int most;

        public int Runs => Volatile.Read(ref runs);
        public int MostAtOnce => Volatile.Read(ref most);

        public async Task JobAsync()
        {
            Interlocked.Increment(ref runs);
            int current = Interlocked.Increment(ref now);
            int seen = Volatile.Read(ref most);
            while (current > seen && Interlocked.CompareExchange(ref most, current, seen) != seen)
            {
                seen = Volatile.Read(ref most);
            }
            await Task.Delay(250); // the "job"
            Interlocked.Decrement(ref now);
        }
    }
}
