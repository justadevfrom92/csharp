using BenchmarkDotNet.Attributes;
using Benchmarks.Builders;

namespace Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class CsvLineBuilderBenchmark
{
    private List<string> values = new();

    // [Params]: the whole benchmark runs once per value, so you see how cost grows with size.
    [Params(10, 1000)]
    public int ValueCount { get; set; }

    // [GlobalSetup]: runs once before measuring, so building the input isn't counted.
    [GlobalSetup]
    public void CreateValues()
    {
        values = Enumerable.Range(0, ValueCount).Select(number => "value" + number).ToList();
    }

    [Benchmark(Baseline = true)]
    public string PlusEquals()
    {
        return CsvLineBuilder.BuildWithPlusEquals(values);
    }

    [Benchmark]
    public string StringBuilder()
    {
        return CsvLineBuilder.BuildWithStringBuilder(values);
    }

    [Benchmark]
    public string Join()
    {
        return CsvLineBuilder.BuildWithJoin(values);
    }
}
