using System.Collections.Concurrent;
using System.Threading.Channels;
using DataflowImport.Models;
using DataflowImport.Parsers;

namespace DataflowImport.Variations;

// The same import as OrderImportPipeline (TPL Dataflow, original) written four other ways.
// Each returns the order ids in the order they were imported, so tests can compare totals AND order.
public static class OrderImportVariation
{
    // Plain loop: one line at a time. Simplest and fully ordered; no parallelism at all.
    public static List<ImportedOrderModel> Sequential(IEnumerable<string> lines)
    {
        var imported = new List<ImportedOrderModel>();
        int number = 0;
        foreach (string line in lines)
        {
            number++;
            ParsedLineModel parsed = OrderLineParser.Parse(number, line);
            if (parsed.IsValid)
            {
                imported.Add(parsed.Order!);
            }
        }
        return imported;
    }

    // Parallel.ForEachAsync: parses 4 at a time with one line of setup. Results arrive in FINISH order, so the
    // imported list is NOT in file order (Dataflow's EnsureOrdered = true keeps it). No batching or routing built in.
    public static async Task<List<ImportedOrderModel>> WithParallelForEachAsync(IEnumerable<string> lines)
    {
        var imported = new ConcurrentQueue<ImportedOrderModel>();
        await Parallel.ForEachAsync(lines.Select((line, index) => (Number: index + 1, Text: line)), new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (line, cancellationToken) =>
        {
            await Task.Delay(Random.Shared.Next(1, 5), cancellationToken); // uneven work, like real parsing
            ParsedLineModel parsed = OrderLineParser.Parse(line.Number, line.Text);
            if (parsed.IsValid)
            {
                imported.Enqueue(parsed.Order!);
            }
        });
        return imported.ToList();
    }

    // PLINQ (AsParallel): parallel LINQ for CPU work on data already in memory. AsOrdered() keeps file order.
    // No async inside, no backpressure: the whole input is processed as one query.
    public static List<ImportedOrderModel> WithPlinq(IEnumerable<string> lines)
    {
        return lines.Select((line, index) => (Number: index + 1, Text: line))
            .AsParallel().AsOrdered().WithDegreeOfParallelism(4)
            .Select(line => OrderLineParser.Parse(line.Number, line.Text))
            .Where(parsed => parsed.IsValid)
            .Select(parsed => parsed.Order!)
            .ToList();
    }

    // Channel + 4 consumer tasks (the 5.2 style): backpressure from the bounded channel, but batching, routing,
    // ordering and passing errors back are all yours to write.
    public static async Task<List<ImportedOrderModel>> WithChannel(IEnumerable<string> lines)
    {
        Channel<(int Number, string Text)> channel = Channel.CreateBounded<(int, string)>(16);
        var imported = new ConcurrentQueue<ImportedOrderModel>();
        Task[] consumers = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            await foreach ((int number, string text) in channel.Reader.ReadAllAsync())
            {
                ParsedLineModel parsed = OrderLineParser.Parse(number, text);
                if (parsed.IsValid)
                {
                    imported.Enqueue(parsed.Order!);
                }
            }
        })).ToArray();
        int lineNumber = 0;
        foreach (string line in lines)
        {
            await channel.Writer.WriteAsync((++lineNumber, line));
        }
        channel.Writer.Complete();
        await Task.WhenAll(consumers);
        return imported.ToList();
    }
}
