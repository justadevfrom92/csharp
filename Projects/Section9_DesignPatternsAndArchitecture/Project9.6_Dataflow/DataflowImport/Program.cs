using DataflowImport.Models;
using DataflowImport.Pipelines;

// 1,000 generated order lines, a few of them broken on purpose.
var lines = new List<string> { "# orderId,customer,quantity,unitPrice" };
for (int number = 1; number <= 1000; number++)
{
    lines.Add(number % 250 == 0 ? $"B-{number},Bad,lots,1.00" : $"A-{number},Customer{number % 7},{number % 5 + 1},{number % 20 + 0.99m}");
}
lines.Add("C-1,,1,1.00");
lines.Add("not,a,valid line");

var savedBatchSizes = new List<int>();
var pipeline = new OrderImportPipeline(batchSize: 100, parallelParsers: 4, saveBatchAsync: async batch =>
{
    await Task.Delay(10); // pretend this is one database round trip
    savedBatchSizes.Add(batch.Count);
});

var timer = System.Diagnostics.Stopwatch.StartNew();
ImportReportModel report = await pipeline.RunAsync(lines);
timer.Stop();

Console.WriteLine($"Imported {report.Imported} orders worth {report.ImportedTotal:N2} in {report.BatchesSaved} batches ({timer.ElapsedMilliseconds} ms)");
Console.WriteLine($"Batch sizes: {string.Join(", ", savedBatchSizes)}");
Console.WriteLine($"Most lines parsed at the same time: {report.MostParsersAtOnce} (limit 4)");
Console.WriteLine($"Rejected {report.Rejected.Count}:");
foreach (string error in report.Rejected)
{
    Console.WriteLine($"  {error}");
}
