using Playground.Models;

namespace Playground.Catalogs;

// Every project the browser can run, in roadmap order. Left out: anything needing threads (5.1), a database
// (6.x, 7.7), a web server (3.x, 5.4, 6.3-6.5, 7.x, 8.x, 9.9, 10.1), a benchmark run (5.3), files from disk
// (9.2, 9.3) or a package feed (4.1).
public static class ProjectCatalog
{
    public static readonly IReadOnlyList<RunnableProjectModel> Projects =
    [
        new("1.1", "Loan & Date Calculator", "SimpleLoanAndDateCalculator", ["--demo"],
            Note: "Runs with --demo: the answers are typed in for you, including wrong ones (\"abc\", 50, 999) to show the validation. Browsers can't feed Console.ReadLine, so typing your own needs dotnet run."),
        new("1.2", "Pattern Matching", "PatternMatching", []),
        new("1.3", "Tuples & Deconstruction", "Tuples", []),
        new("1.4", "Records, Sealed & Extensions", "Records", []),
        new("2.1", "Warehouse Inventory & Orders", "InventoryAnalytics", []),
        new("2.2", "Iterators (yield)", "Iterators", []),
        new("4.2", "DDD Banking", "Banking.Demo", []),
        new("5.2", "Channel Log Pipeline", "ChannelLogPipeline", [],
            Note: "Browsers run .NET on ONE thread, so the \"parallel\" consumers take turns instead of running at once."),
        new("9.1", "Operators & Lambdas", "OperatorsAndLambdas", []),
        new("9.4", "Strategy, Command, State, Mediator, Observer", "BehavioralPatterns", []),
        new("9.5", "Pipes & Filters, Chain, Decorator, Interceptor", "PipelinePatterns", []),
        new("9.6", "Dataflow (TPL Dataflow)", "DataflowImport", [],
            Note: "One thread in the browser: \"most lines parsed at the same time\" shows what really happened."),
        new("9.7", "Workflow Engine", "WorkflowEngine", []),
        new("9.8", "Reactive Extensions (Rx)", "ReactiveExtensions", []),
    ];
}
