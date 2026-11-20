using AsyncStreams.Generators;
using AsyncStreams.Models;

namespace AsyncStreams.Tests;

public class PriceTickGeneratorTest
{
    private static readonly TimeSpan Fast = TimeSpan.FromMilliseconds(5);

    [Fact]
    public async Task Stream_PositivePositive_TicksArriveInOrder()
    {
        var ticks = new List<PriceTickModel>();
        await foreach (PriceTickModel tick in PriceTickGenerator.StreamAsync("ACME", 3, Fast))
        {
            ticks.Add(tick);
        }

        Assert.Equal([new PriceTickModel(1, "ACME", 102m), new PriceTickModel(2, "ACME", 100.5m), new PriceTickModel(3, "ACME", 102.5m)], ticks);
    }

    [Fact]
    public async Task Stream_PositiveNegative_ZeroCountIsEmpty_CancelHalfwayKeepsWhatArrived()
    {
        using var cancel = new CancellationTokenSource();
        var received = new List<int>();

        Assert.Empty(await PriceTickGenerator.StreamAsync("ACME", 0, Fast).ToListAsync());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (PriceTickModel tick in PriceTickGenerator.StreamAsync("ACME", 10, Fast).WithCancellation(cancel.Token))
            {
                received.Add(tick.Number);
                if (tick.Number == 2)
                {
                    cancel.Cancel();
                }
            }
        });
        Assert.Equal([1, 2], received);
    }

    [Fact]
    public async Task Stream_NegativeNegative_AlreadyCancelled_ThrowsAtTheFirstMoveNext_NotAtTheCall()
    {
        IAsyncEnumerable<PriceTickModel> ticks = PriceTickGenerator.StreamAsync("ACME", 3, Fast, new CancellationToken(canceled: true)); // no error yet
        IAsyncEnumerator<PriceTickModel> cursor = ticks.GetAsyncEnumerator();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await cursor.MoveNextAsync());
        await cursor.DisposeAsync();
    }
}
