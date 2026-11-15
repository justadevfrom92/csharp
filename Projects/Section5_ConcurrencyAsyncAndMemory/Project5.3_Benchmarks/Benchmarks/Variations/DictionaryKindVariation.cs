using System.Collections.Frozen;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Variations;

// Four "key -> value" collections answering the same question. LookupBenchmark (original) compares
// List vs HashSet vs Dictionary; this compares DICTIONARY KINDS. Run:
//   dotnet run -c Release -- --filter "*DictionaryKindVariation*" --job short
[MemoryDiagnoser]
public class DictionaryKindVariation
{
    private Dictionary<string, int> dictionary = new();
    private FrozenDictionary<string, int> frozen = FrozenDictionary<string, int>.Empty;
    private SortedDictionary<string, int> sorted = new();
    private ImmutableDictionary<string, int> immutable = ImmutableDictionary<string, int>.Empty;
    private string key = "";

    [Params(1_000)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void Fill()
    {
        dictionary = Enumerable.Range(0, ItemCount).ToDictionary(number => "SKU-" + number, number => number);
        frozen = dictionary.ToFrozenDictionary();        // slow to BUILD, fastest to READ; can never change
        sorted = new SortedDictionary<string, int>(dictionary); // keys kept in ORDER; lookups are O(log n)
        immutable = dictionary.ToImmutableDictionary();  // "change" = a new copy; old copies stay valid
        key = "SKU-" + (ItemCount / 2);
    }

    [Benchmark(Baseline = true)]
    public int Dictionary()
    {
        return dictionary[key];
    }

    [Benchmark]
    public int Frozen()
    {
        return frozen[key];
    }

    [Benchmark]
    public int Sorted()
    {
        return sorted[key];
    }

    [Benchmark]
    public int Immutable()
    {
        return immutable[key];
    }
}
