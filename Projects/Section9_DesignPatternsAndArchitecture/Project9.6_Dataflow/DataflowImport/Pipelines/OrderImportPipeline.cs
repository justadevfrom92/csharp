using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using DataflowImport.Models;
using DataflowImport.Parsers;

namespace DataflowImport.Pipelines;

// DATAFLOW ARCHITECTURE: the program is a graph of BLOCKS; data FLOWS through the links between them.
// Each block does one job, has its own input queue, and can run several items in PARALLEL.
//
//   lines ──► [parse x4 in parallel] ──valid──► [batch of N] ──► [save batch]
//                                    └─invalid─► [collect error]
//
// Compared with 5.2 (one Channel, hand-written loops) Dataflow gives ready-made blocks for parallelism,
// batching, routing, backpressure and completion. Compared with 9.5's pipes and filters: those run one item
// through all steps on one thread; here MANY items are in different blocks at the same time.
public class OrderImportPipeline
{
    private readonly int batchSize;
    private readonly int parallelParsers;
    private readonly Func<IReadOnlyList<ImportedOrderModel>, Task> saveBatchAsync;

    public OrderImportPipeline(int batchSize, int parallelParsers, Func<IReadOnlyList<ImportedOrderModel>, Task> saveBatchAsync)
    {
        if (batchSize < 1 || parallelParsers < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size and parser count must be at least 1.");
        }
        ArgumentNullException.ThrowIfNull(saveBatchAsync);
        this.batchSize = batchSize;
        this.parallelParsers = parallelParsers;
        this.saveBatchAsync = saveBatchAsync;
    }

    public async Task<ImportReportModel> RunAsync(IEnumerable<string> lines, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var rejected = new ConcurrentBag<ParsedLineModel>();
        int imported = 0;
        decimal importedTotal = 0m;
        int batchesSaved = 0;
        int parsingNow = 0;
        int mostParsingAtOnce = 0;
        object totalsLock = new object();

        // BoundedCapacity = backpressure: at most this many items wait in a block. When it's full,
        // SendAsync below WAITS, so a huge file is never loaded into memory all at once.
        var parse = new TransformBlock<(int Number, string Text), ParsedLineModel>(async line =>
        {
            int now = Interlocked.Increment(ref parsingNow);
            InterlockedMax(ref mostParsingAtOnce, now);
            await Task.Delay(5, cancellationToken); // pretend parsing takes a moment (e.g. looking up the customer)
            Interlocked.Decrement(ref parsingNow);
            return OrderLineParser.Parse(line.Number, line.Text);
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = parallelParsers,
            BoundedCapacity = parallelParsers * 4,
            EnsureOrdered = true,              // results leave in the order lines came in, even though they ran in parallel
            CancellationToken = cancellationToken,
        });

        var batch = new BatchBlock<ImportedOrderModel>(batchSize, new GroupingDataflowBlockOptions { BoundedCapacity = batchSize * 2 });

        var save = new ActionBlock<ImportedOrderModel[]>(async orders =>
        {
            await saveBatchAsync(orders);  // e.g. one database INSERT per batch instead of one per order
            lock (totalsLock)
            {
                imported += orders.Length;
                importedTotal += orders.Sum(order => order.Total);
                batchesSaved++;
            }
        },
        new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, BoundedCapacity = 2, CancellationToken = cancellationToken });

        var collectError = new ActionBlock<ParsedLineModel>(result => rejected.Add(result));

        // LINKS. PropagateCompletion: when a block finishes, the next one is told "no more data is coming".
        var propagate = new DataflowLinkOptions { PropagateCompletion = true };
        var toValid = new TransformBlock<ParsedLineModel, ImportedOrderModel>(result => result.Order!);
        parse.LinkTo(toValid, propagate, result => result.IsValid);     // ROUTING by a condition...
        parse.LinkTo(collectError, propagate, result => !result.IsValid); // ...every item matches exactly one link
        toValid.LinkTo(batch, propagate);
        batch.LinkTo(save, propagate);

        // Completion only flows FORWARD along links. If saving FAILS, the blocks before it don't know, and keep
        // parsing every remaining line for nothing (measured in the test: ~4 s of wasted work for 1,000 lines
        // instead of stopping at once). So a failure at the end is passed back to the start: the parse block is
        // faulted, SendAsync then returns false, and we stop feeding lines.
        _ = save.Completion.ContinueWith(
            failed => ((IDataflowBlock)parse).Fault(failed.Exception!.InnerException!),
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        int lineNumber = 0;
        foreach (string line in lines)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }
            bool accepted = await parse.SendAsync((lineNumber, line), cancellationToken); // waits while the block is full
            if (!accepted)
            {
                break; // the pipeline has failed; the real error is thrown below
            }
        }
        parse.Complete(); // no more input -> completion flows down the links; BatchBlock flushes its last, smaller batch

        // Wait for BOTH ends of the graph. If any block failed, this throws its exception.
        await Task.WhenAll(save.Completion, collectError.Completion);

        return new ImportReportModel(imported, importedTotal, batchesSaved, rejected.OrderBy(result => result.LineNumber).Select(result => result.Error!).ToList(), mostParsingAtOnce);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current = Volatile.Read(ref target);
        while (value > current && Interlocked.CompareExchange(ref target, value, current) != current)
        {
            current = Volatile.Read(ref target);
        }
    }
}
