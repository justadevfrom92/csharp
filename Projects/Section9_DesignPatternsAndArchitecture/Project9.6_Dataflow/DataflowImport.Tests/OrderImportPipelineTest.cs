using DataflowImport.Models;
using DataflowImport.Pipelines;

namespace DataflowImport.Tests;

// PositivePositive = valid input -> works; PositiveNegative = well-formed input that breaks a rule -> refused;
// NegativeNegative = broken or missing input -> rejected.
public class OrderImportPipelineTest
{
    private readonly List<IReadOnlyList<ImportedOrderModel>> savedBatches = new();

    private OrderImportPipeline Pipeline(int batchSize = 10, int parsers = 3)
    {
        return new OrderImportPipeline(batchSize, parsers, batch =>
        {
            lock (savedBatches)
            {
                savedBatches.Add(batch.ToList());
            }
            return Task.CompletedTask;
        });
    }

    private static List<string> ValidLines(int count)
    {
        return Enumerable.Range(1, count).Select(number => $"A-{number},Ann,2,1.50").ToList();
    }

    [Fact]
    public async Task Run_PositivePositive_ValidLines_AreSavedInBatches_InOrder_WithLimitedParallelism()
    {
        ImportReportModel report = await Pipeline(batchSize: 10, parsers: 3).RunAsync(ValidLines(25));

        Assert.Equal(25, report.Imported);
        Assert.Equal(75m, report.ImportedTotal);                       // 25 x 2 x 1.50
        Assert.Equal([10, 10, 5], savedBatches.Select(batch => batch.Count)); // last batch flushed on Complete()
        Assert.Equal(Enumerable.Range(1, 25).Select(number => $"A-{number}"), savedBatches.SelectMany(batch => batch).Select(order => order.OrderId));
        Assert.InRange(report.MostParsersAtOnce, 1, 3);
        Assert.Empty(report.Rejected);
    }

    [Fact]
    public async Task Run_PositiveNegative_InvalidLines_AreRoutedToRejected_ValidOnesStillImported()
    {
        List<string> lines = ValidLines(3);
        lines.Add("B-1,Bob,0,1.00");       // quantity 0
        lines.Add("B-2,,1,1.00");          // no customer
        lines.Add("# a comment");          // skipped, not rejected
        lines.Add("");                     // skipped

        ImportReportModel report = await Pipeline().RunAsync(lines);

        Assert.Equal(3, report.Imported);
        Assert.Equal(["line 4: quantity must be a whole number above 0", "line 5: order id and customer are required"], report.Rejected);
    }

    [Fact]
    public async Task Run_PositiveNegative_NoLines_SavesNothing()
    {
        ImportReportModel report = await Pipeline().RunAsync([]);

        Assert.Equal(0, report.Imported);
        Assert.Equal(0, report.BatchesSaved);
        Assert.Empty(savedBatches);
    }

    [Fact]
    public async Task Run_NegativeNegative_SavingFails_ErrorReachesCallerQuickly()
    {
        var failing = new OrderImportPipeline(batchSize: 2, parallelParsers: 2, _ => throw new IOException("database down"));

        // 1,000 lines take ~2.5 s to parse; stopping early after the first failed batch takes well under 2 s.
        var error = await Assert.ThrowsAsync<IOException>(() => failing.RunAsync(ValidLines(1000)).WaitAsync(TimeSpan.FromSeconds(2)));

        Assert.Equal("database down", error.Message);
    }

    [Fact]
    public async Task Run_NegativeNegative_Cancelled_Stops()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Pipeline(batchSize: 10, parsers: 1).RunAsync(ValidLines(10_000), cancellation.Token));
    }

    [Fact]
    public async Task Run_NegativeNegative_NullInputOrBadSettings_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Pipeline().RunAsync(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderImportPipeline(0, 1, _ => Task.CompletedTask));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderImportPipeline(1, 0, _ => Task.CompletedTask));
        Assert.Throws<ArgumentNullException>(() => new OrderImportPipeline(1, 1, null!));
    }
}
