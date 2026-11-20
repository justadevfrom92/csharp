using System.Reflection;
using Benchmarks.Variations;

namespace Benchmarks.Tests;

// A benchmark is only fair if every method computes the SAME answer - otherwise you're timing different work.
public class VariationsTest
{
    private static DictionaryKindVariation Ready(int itemCount)
    {
        var benchmark = new DictionaryKindVariation { ItemCount = itemCount };
        benchmark.Fill();
        return benchmark;
    }

    [Fact]
    public void DictionaryKinds_PositivePositive_AllFourReturnTheSameValue()
    {
        DictionaryKindVariation benchmark = Ready(1_000);

        Assert.Equal(500, benchmark.Dictionary());
        Assert.Equal(500, benchmark.Frozen());
        Assert.Equal(500, benchmark.Sorted());
        Assert.Equal(500, benchmark.Immutable());
    }

    [Fact]
    public void DictionaryKinds_PositiveNegative_SmallestSize_StillAgree()
    {
        DictionaryKindVariation benchmark = Ready(1); // key "SKU-0"

        Assert.Equal(0, benchmark.Dictionary());
        Assert.Equal(benchmark.Dictionary(), benchmark.Frozen());
        Assert.Equal(benchmark.Dictionary(), benchmark.Sorted());
        Assert.Equal(benchmark.Dictionary(), benchmark.Immutable());
    }

    [Fact]
    public void DictionaryKinds_NegativeNegative_EmptyCollections_AllThrowKeyNotFound()
    {
        DictionaryKindVariation benchmark = Ready(0); // key "SKU-0" doesn't exist

        Assert.Throws<KeyNotFoundException>(() => benchmark.Dictionary());
        Assert.Throws<KeyNotFoundException>(() => benchmark.Frozen());
        Assert.Throws<KeyNotFoundException>(() => benchmark.Sorted());
        Assert.Throws<KeyNotFoundException>(() => benchmark.Immutable());
    }
    private static ReflectionCostVariation ReadyReflection()
    {
        var benchmark = new ReflectionCostVariation();
        benchmark.Prepare();
        return benchmark;
    }

    [Fact]
    public void ReflectionCost_PositivePositive_AllFourReturnTheSameLength()
    {
        ReflectionCostVariation benchmark = ReadyReflection();

        Assert.Equal(9, benchmark.Direct());
        Assert.Equal(9, benchmark.CachedDelegate());
        Assert.Equal(9, benchmark.CachedPropertyInfo());
        Assert.Equal(9, benchmark.LookupEveryTime());
    }

    [Fact]
    public void ReflectionCost_PositiveNegative_WithoutSetup_TheDelegateFieldStillHoldsItsPlaceholder()
    {
        var benchmark = new ReflectionCostVariation(); // Prepare() not called: text is "", delegate is the dummy

        Assert.Equal(0, benchmark.Direct());
        Assert.Equal(0, benchmark.CachedDelegate());
        Assert.Equal(0, benchmark.CachedPropertyInfo());
    }

    [Fact]
    public void ReflectionCost_NegativeNegative_WrongNameOrWrongDelegateShape_FailsAtRunTime()
    {
        Assert.Null(typeof(string).GetProperty("Lenght")); // a typo isn't a compile error, just null
        MethodInfo getter = typeof(string).GetProperty("Length")!.GetMethod!;
        Assert.Throws<ArgumentException>(() => getter.CreateDelegate<Func<string, long>>()); // returns int, not long
    }
}
