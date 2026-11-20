using AsyncStreams.Variations;

namespace AsyncStreams.Tests;

// Variations/CancellationVariation.cs: which async iterators actually stop when cancelled.
public class VariationsTest
{
    private static async Task<List<int>> ReadAll(IAsyncEnumerable<int> numbers, CancellationToken cancellationToken)
    {
        var received = new List<int>();
        await foreach (int number in numbers.WithCancellation(cancellationToken))
        {
            received.Add(number);
        }
        return received;
    }

    [Fact]
    public async Task Cancellation_PositivePositive_NotCancelled_AllThreeGiveEverything()
    {
        Assert.Equal([1, 2, 3], await ReadAll(CancellationVariation.WithoutToken(3), CancellationToken.None));
        Assert.Equal([1, 2, 3], await ReadAll(CancellationVariation.WithAttribute(3), CancellationToken.None));
        Assert.Equal([1, 2, 3], await ReadAll(CancellationVariation.StopQuietly(3), CancellationToken.None));
    }

    [Fact]
    public async Task Cancellation_PositiveNegative_Cancelled_NoTokenIgnoresIt_QuietOneStopsSilently()
    {
        var cancelled = new CancellationToken(canceled: true);

        Assert.Equal([1, 2, 3], await ReadAll(CancellationVariation.WithoutToken(3), cancelled)); // WithCancellation ignored
        Assert.Empty(await ReadAll(CancellationVariation.StopQuietly(3), cancelled));             // no exception
    }

    [Fact]
    public async Task Cancellation_NegativeNegative_Cancelled_AttributeVersionThrows()
    {
        var cancelled = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadAll(CancellationVariation.WithAttribute(3), cancelled));
    }
}
