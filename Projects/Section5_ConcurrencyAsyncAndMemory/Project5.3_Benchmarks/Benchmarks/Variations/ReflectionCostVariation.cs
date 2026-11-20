using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Variations;

// Reading ONE property four ways: how much does reflection cost? Run:
//   dotnet run -c Release -- --filter "*ReflectionCostVariation*" --job short
[MemoryDiagnoser]
public class ReflectionCostVariation
{
    private string text = "";
    private PropertyInfo lengthProperty = typeof(string).GetProperty("Length")!;
    private Func<string, int> lengthGetter = value => 0;

    [GlobalSetup]
    public void Prepare()
    {
        text = "SKU-12345";
        lengthProperty = typeof(string).GetProperty("Length")!;
        // Turn the property's hidden "get_Length" method into a normal delegate ONCE; calls are then almost direct.
        lengthGetter = lengthProperty.GetMethod!.CreateDelegate<Func<string, int>>();
    }

    [Benchmark(Baseline = true)]
    public int Direct()
    {
        return text.Length;
    }

    [Benchmark]
    public int CachedDelegate()
    {
        return lengthGetter(text);
    }

    // GetValue returns object, so the int is BOXED (an allocation) and must be cast back.
    [Benchmark]
    public int CachedPropertyInfo()
    {
        return (int)lengthProperty.GetValue(text)!;
    }

    // The slowest habit: looking the property up by name on every call.
    [Benchmark]
    public int LookupEveryTime()
    {
        return (int)typeof(string).GetProperty("Length")!.GetValue(text)!;
    }
}
