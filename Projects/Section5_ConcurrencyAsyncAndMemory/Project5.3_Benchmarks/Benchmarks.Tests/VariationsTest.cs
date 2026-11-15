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
}
