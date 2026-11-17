# Project 9.6 — Dataflow Architecture (TPL Dataflow)

Import 1,000 CSV order lines: parse them **in parallel**, send bad lines one way and good lines another,
group the good ones into **batches**, and save each batch — with **backpressure** so a huge file never sits in
memory all at once.

```bash
cd DataflowImport
dotnet run
```

```bash
cd DataflowImport.Tests
dotnet test
```

## What "dataflow" means

The program is a **graph of blocks**. Each block does one job and has its own input queue; data **flows** along
the links. Many items are in different blocks **at the same time**.

```
lines ──SendAsync──► [TransformBlock: parse, 4 at once] ──IsValid──►   [TransformBlock] ─► [BatchBlock 100] ─► [ActionBlock: save]
                                                       └─!IsValid──► [ActionBlock: collect error]
```

| Block | Job |
|---|---|
| `TransformBlock<TIn, TOut>` | one item in → one item out (can run N in parallel) |
| `TransformManyBlock<TIn, TOut>` | one item in → many out (e.g. a file → its lines) |
| `BatchBlock<T>` | collects N items → one array |
| `ActionBlock<T>` | the end: does something with each item |
| `BufferBlock<T>` / `BroadcastBlock<T>` | plain queue / copy to every target |

**`TPL Dataflow` is part of .NET** (`System.Threading.Tasks.Dataflow`) — no package needed.

## The options that matter

| Setting | Effect here |
|---|---|
| `MaxDegreeOfParallelism = 4` | at most 4 lines parsed at once (the demo prints the highest seen: 4) |
| `EnsureOrdered = true` | results come out in input order even though they ran in parallel |
| `BoundedCapacity` | **backpressure**: a full block makes `SendAsync` wait instead of queueing the whole file in memory |
| `LinkTo(target, options, predicate)` | **routing**: valid lines to the batcher, invalid ones to the error collector. Every item must match some link, or it gets stuck |
| `PropagateCompletion = true` | when a block finishes, the next one is told; `BatchBlock` then **flushes its last, smaller batch** (`..., 100, 96`) |
| `Complete()` + `await block.Completion` | "no more input", then wait for the end of the graph; a failed block's exception comes out here |

## A problem found while building it

Completion and errors only flow **forward** along links. When saving failed, the earlier blocks didn't know and
kept parsing every remaining line for nothing (the 1,000-line test took ~4 s instead of stopping at once). The fix:
when the save block faults, **fault the first block too** (`((IDataflowBlock)parse).Fault(...)`), so `SendAsync`
returns `false` and the loop stops. The test `SavingFails_ErrorReachesCallerQuickly` fails without that fix.

## Dataflow vs the other pipelines in this repo

| | Use it for |
|---|---|
| LINQ (2.1) | one-off transformations of data already in memory |
| `Channel<T>` (5.2) | one producer/consumer queue; you write the loops |
| Pipes and filters (9.5) | small steps, one item at a time, one thread |
| **Dataflow (this)** | several stages, each with its own parallelism, plus batching, routing and backpressure |
| Rx (9.8) | events arriving over **time** (ticks, clicks), with time operators like throttle and buffer-by-time |

## Code snippets

**A parallel, bounded transform block**, from `Pipelines/OrderImportPipeline.cs`

```csharp
var parse = new TransformBlock<(int Number, string Text), ParsedLineModel>(async line =>
{
    await Task.Delay(5, cancellationToken);
    return OrderLineParser.Parse(line.Number, line.Text);
},
new ExecutionDataflowBlockOptions
{
    MaxDegreeOfParallelism = parallelParsers,
    BoundedCapacity = parallelParsers * 4,
    EnsureOrdered = true,
    CancellationToken = cancellationToken,
});
```

**Batching and the final action**

```csharp
var batch = new BatchBlock<ImportedOrderModel>(batchSize, new GroupingDataflowBlockOptions { BoundedCapacity = batchSize * 2 });

var save = new ActionBlock<ImportedOrderModel[]>(async orders =>
{
    await saveBatchAsync(orders);
},
new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1, BoundedCapacity = 2 });
```

**Linking with routing and completion**

```csharp
var propagate = new DataflowLinkOptions { PropagateCompletion = true };
parse.LinkTo(toValid, propagate, result => result.IsValid);
parse.LinkTo(collectError, propagate, result => !result.IsValid);
toValid.LinkTo(batch, propagate);
batch.LinkTo(save, propagate);
```

**Feeding it, finishing it, waiting for it**

```csharp
foreach (string line in lines)
{
    bool accepted = await parse.SendAsync((lineNumber, line), cancellationToken); // waits while full
    if (!accepted)
    {
        break;
    }
}
parse.Complete();
await Task.WhenAll(save.Completion, collectError.Completion);
```

**Passing a failure back to the start**

```csharp
_ = save.Completion.ContinueWith(
    failed => ((IDataflowBlock)parse).Fault(failed.Exception!.InnerException!),
    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
```

## Variations

The same import (200 valid lines + 2 broken) written four other ways, in `DataflowImport/Variations/OrderImportVariation.cs`.
`DataflowImport.Tests/VariationsTest.cs` checks that all five import the same 200 orders and the same total.

| Way | Parallel? | Keeps file order? | Backpressure | Batching / routing / errors back to the start | The difference |
|---|---|---|---|---|---|
| TPL Dataflow (original) | ✅ per stage | ✅ `EnsureOrdered` | ✅ `BoundedCapacity` | ✅ built in (`BatchBlock`, `LinkTo` predicates) | most to learn; most built in |
| plain `foreach` (`Sequential`) | ❌ | ✅ | — | write it yourself | simplest; slowest with slow steps |
| `Parallel.ForEachAsync` (`WithParallelForEachAsync`) | ✅ 4 at once | **❌ finish order** (the test checks it differs) | ❌ | write it yourself | one line of setup |
| PLINQ `AsParallel().AsOrdered()` (`WithPlinq`) | ✅ | ✅ with `AsOrdered()` | ❌ | ❌ | for **CPU** work on data already in memory; no `await` inside |
| `Channel` + 4 consumer tasks (`WithChannel`, the 5.2 style) | ✅ | ❌ | ✅ bounded channel | write it yourself | the building block Dataflow is made of |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `ParsedLineModel` holding an order **or** an error | One output type lets the parse block route by `IsValid`. | Throwing from the block would fault the whole pipeline over one bad line. |
| `(int Number, string Text)` tuple input | Small, local, keeps the line number with the text. | A class for two values used in one place. |
| `ConcurrentBag<ParsedLineModel>` for rejects | Written from a block; sorted by line number at the end. | A `List` would need a lock. |
| `lock` around the totals | Three values updated together. | `Interlocked` only works for one value at a time. |
| `Func<IReadOnlyList<ImportedOrderModel>, Task> saveBatchAsync` | The pipeline doesn't care how batches are saved; tests pass a fake. | Hard-coding a database. |
| `record` models | Plain data passed between blocks; never changed. | — |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Import | 25 lines → batches 10, 10, 5, in order, ≤ 3 parsing at once | bad lines routed to rejected (comments/blank lines skipped); no lines → nothing saved | save fails → error reaches the caller in < 2 s; cancellation stops it; `null` input; batch size / parser count 0; `null` save function |
