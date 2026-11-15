using RateLimiting.Variations;

namespace RateLimiting.Tests;

// The four rate-limiting algorithms on the same burst (RateLimiting/Variations/LimiterAlgorithmVariation.cs):
// 4 requests at the end of a window, then up to 10 more right after time moves on one step.
public class VariationsTest
{
    [Fact]
    public void Limiters_PositivePositive_FixedWindow_ResetsCompletely_AllowingADoubleBurst()
    {
        Assert.Equal(4, LimiterAlgorithmVariation.AllowedRightAfterBoundaryWithFixedWindow()); // 4 + 4 in a moment
    }

    [Fact]
    public void Limiters_PositiveNegative_SlidingWindowAndTokenBucket_SmoothTheBoundary()
    {
        Assert.Equal(0, LimiterAlgorithmVariation.AllowedRightAfterBoundaryWithSlidingWindow()); // old requests still count
        // Token bucket refills by elapsed time (2 per 100 ms): ~150 ms gives 3; a slow machine may refill up to the 4-token cap.
        Assert.InRange(LimiterAlgorithmVariation.AllowedRightAfterBoundaryWithTokenBucket(), 2, 4);
    }

    [Fact]
    public void Limiters_NegativeNegative_ConcurrencyLimiter_RefusesAThirdWhileTwoRun_ThenAllowsOneWhenOneFinishes()
    {
        (int whileTwoRunning, int afterOneFinished) = LimiterAlgorithmVariation.ConcurrencyLimiter();

        Assert.Equal(0, whileTwoRunning);
        Assert.Equal(1, afterOneFinished);
    }
}
