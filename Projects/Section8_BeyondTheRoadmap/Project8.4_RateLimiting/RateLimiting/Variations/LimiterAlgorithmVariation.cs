using System.Threading.RateLimiting;

namespace RateLimiting.Variations;

// The four built-in rate-limiting ALGORITHMS, all set to "4 requests", compared on the same burst:
// 4 requests at the very END of one time window, then as many as possible right after the window moves on.
// Windows are 100 ms so the demo is quick. AutoReplenishment = false + TryReplenish() decide EXACTLY when the limiter
// moves on (TryReplenish only works once the period has really passed - found while writing this - so we wait 150 ms first).
public static class LimiterAlgorithmVariation
{
    // ORIGINAL (Program.cs uses a fixed window per client): the count resets at each window boundary, so a
    // client can do 4 at 0:09 and 4 more at 0:10 - 8 requests in about one second.
    public static int AllowedRightAfterBoundaryWithFixedWindow()
    {
        using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 4,
            Window = TimeSpan.FromMilliseconds(100),
            AutoReplenishment = false,
        });
        return BurstAcrossBoundary(limiter);
    }

    // SLIDING window split into 2 segments: requests are only forgotten one FULL window after they happened,
    // so the burst at the end of the old window still counts - no double burst at the boundary.
    public static int AllowedRightAfterBoundaryWithSlidingWindow()
    {
        using var limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 4,
            Window = TimeSpan.FromMilliseconds(200),
            SegmentsPerWindow = 2, // each segment = 100 ms
            AutoReplenishment = false,
        });
        return BurstAcrossBoundary(limiter);
    }

    // TOKEN BUCKET: holds up to 4 tokens, refills 2 per 100 ms period. Allows short bursts, then a steady average rate.
    // It refills by the time that REALLY passed: after the 150 ms wait that's 1.5 periods = 3 tokens (never more than 4).
    public static int AllowedRightAfterBoundaryWithTokenBucket()
    {
        using var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 4,
            TokensPerPeriod = 2,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(100),
            AutoReplenishment = false,
        });
        return BurstAcrossBoundary(limiter);
    }

    // CONCURRENCY limiter (original, for /api/report): not about TIME at all - at most 2 requests RUNNING at once.
    // A permit comes back as soon as a request finishes (its lease is disposed).
    public static (int WhileTwoRunning, int AfterOneFinished) ConcurrencyLimiter()
    {
        using var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions { PermitLimit = 2, QueueLimit = 0 });
        RateLimitLease first = limiter.AttemptAcquire();
        RateLimitLease second = limiter.AttemptAcquire();
        using RateLimitLease third = limiter.AttemptAcquire();
        int whileTwoRunning = third.IsAcquired ? 1 : 0;
        first.Dispose(); // request 1 finished
        using RateLimitLease fourth = limiter.AttemptAcquire();
        second.Dispose();
        return (whileTwoRunning, fourth.IsAcquired ? 1 : 0);
    }

    private static int BurstAcrossBoundary(ReplenishingRateLimiter limiter)
    {
        for (int i = 0; i < 4; i++)
        {
            using RateLimitLease lease = limiter.AttemptAcquire(); // 4 requests at the end of the first window
        }
        Thread.Sleep(150);                                          // let one window / segment / period pass...
        limiter.TryReplenish();                                     // ...and move the limiter on exactly once
        int allowed = 0;
        for (int i = 0; i < 10; i++)
        {
            using RateLimitLease lease = limiter.AttemptAcquire();
            if (lease.IsAcquired)
            {
                allowed++;
            }
        }
        return allowed;
    }
}
