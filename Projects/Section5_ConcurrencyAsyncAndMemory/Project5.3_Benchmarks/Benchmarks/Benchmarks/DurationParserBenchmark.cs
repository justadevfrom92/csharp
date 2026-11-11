using BenchmarkDotNet.Attributes;
using Benchmarks.Parsers;

namespace Benchmarks.Benchmarks;

[MemoryDiagnoser] // also report how much memory each method allocates
public class DurationParserBenchmark
{
    private const string LogLine = "2026-11-05T10:00:00.0000000Z ERROR 1234 /api/orders";

    [Benchmark(Baseline = true)] // the other results are shown relative to this one
    public int Split()
    {
        return DurationParser.ParseWithSplit(LogLine);
    }

    [Benchmark]
    public int Span()
    {
        return DurationParser.ParseWithSpan(LogLine);
    }
}
