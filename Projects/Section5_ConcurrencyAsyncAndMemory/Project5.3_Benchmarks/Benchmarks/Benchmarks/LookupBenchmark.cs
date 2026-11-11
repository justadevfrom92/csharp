using BenchmarkDotNet.Attributes;

namespace Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class LookupBenchmark
{
    private List<string> list = new();
    private HashSet<string> hashSet = new();
    private Dictionary<string, int> dictionary = new();
    private string lastCode = "";

    [Params(100, 100_000)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void FillCollections()
    {
        list = Enumerable.Range(0, ItemCount).Select(number => "SKU-" + number).ToList();
        hashSet = new HashSet<string>(list);
        dictionary = list.ToDictionary(code => code, code => code.Length);
        lastCode = "SKU-" + (ItemCount - 1); // worst case for the List: it's at the very end
    }

    [Benchmark(Baseline = true)]
    public bool ListContains()
    {
        return list.Contains(lastCode);
    }

    [Benchmark]
    public bool HashSetContains()
    {
        return hashSet.Contains(lastCode);
    }

    [Benchmark]
    public bool DictionaryContainsKey()
    {
        return dictionary.ContainsKey(lastCode);
    }
}
