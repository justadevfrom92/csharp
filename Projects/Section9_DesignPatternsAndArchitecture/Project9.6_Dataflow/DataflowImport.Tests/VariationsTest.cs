using DataflowImport.Models;
using DataflowImport.Pipelines;
using DataflowImport.Variations;

namespace DataflowImport.Tests;

// The same import, five ways (DataflowImport/Variations/OrderImportVariation.cs vs OrderImportPipeline).
public class VariationsTest
{
    private static List<string> Lines()
    {
        var lines = Enumerable.Range(1, 200).Select(number => $"A-{number},Ann,{number % 3 + 1},2.50").ToList();
        lines.Add("B-1,Bob,0,1.00");     // invalid: quantity 0
        lines.Add("not a line");          // invalid
        return lines;
    }

    [Fact]
    public async Task Import_PositivePositive_EveryWay_ImportsTheSameOrdersAndTotal()
    {
        decimal pipelineTotal = 0;
        var pipeline = new OrderImportPipeline(50, 4, batch =>
        {
            lock (totalLock)
            {
                pipelineTotal += batch.Sum(order => order.Total);
            }
            return Task.CompletedTask;
        });
        ImportReportModel report = await pipeline.RunAsync(Lines());

        foreach (List<ImportedOrderModel> imported in new[]
        {
            OrderImportVariation.Sequential(Lines()),
            await OrderImportVariation.WithParallelForEachAsync(Lines()),
            OrderImportVariation.WithPlinq(Lines()),
            await OrderImportVariation.WithChannel(Lines()),
        })
        {
            Assert.Equal(report.Imported, imported.Count);
            Assert.Equal(report.ImportedTotal, imported.Sum(order => order.Total));
        }
        Assert.Equal(200, report.Imported);
    }

    [Fact]
    public async Task Import_PositiveNegative_OnlySomeWaysKeepFileOrder()
    {
        List<string> expected = Enumerable.Range(1, 200).Select(number => $"A-{number}").ToList();

        Assert.Equal(expected, OrderImportVariation.Sequential(Lines()).Select(order => order.OrderId));
        Assert.Equal(expected, OrderImportVariation.WithPlinq(Lines()).Select(order => order.OrderId));          // AsOrdered()
        Assert.NotEqual(expected, (await OrderImportVariation.WithParallelForEachAsync(Lines())).Select(order => order.OrderId)); // finish order
    }

    [Fact]
    public async Task Import_NegativeNegative_NoValidLines_EveryWayImportsNothing()
    {
        string[] bad = ["x", "y,z", ",,,"];

        Assert.Empty(OrderImportVariation.Sequential(bad));
        Assert.Empty(await OrderImportVariation.WithParallelForEachAsync(bad));
        Assert.Empty(OrderImportVariation.WithPlinq(bad));
        Assert.Empty(await OrderImportVariation.WithChannel(bad));
    }

    private static readonly object totalLock = new object();
}
