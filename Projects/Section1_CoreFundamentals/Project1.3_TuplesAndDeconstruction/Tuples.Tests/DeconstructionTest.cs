using Tuples.Models;

namespace Tuples.Tests;

public class DeconstructionTest
{
    [Fact]
    public void Deconstruct_PositivePositive_ClassDictionaryAndSwap()
    {
        var (weight, destination) = new ParcelModel(2.5m, "Paris");
        var seen = new List<string>();
        foreach (var (product, quantity) in new Dictionary<string, int> { ["Tea"] = 12 })
        {
            seen.Add($"{product}={quantity}");
        }
        int left = 1;
        int right = 2;
        (left, right) = (right, left);

        Assert.Equal((2.5m, "Paris"), (weight, destination));
        Assert.Equal(["Tea=12"], seen);
        Assert.Equal((2, 1), (left, right));
    }

    [Fact]
    public void Deconstruct_PositiveNegative_DiscardsSkipValues_PositionalPatternMatches()
    {
        var (_, destination) = new ParcelModel(30m, "Oslo");
        string size = new ParcelModel(30m, "Oslo") switch
        {
            (> 20m, _) => "heavy",
            _ => "normal",
        };

        Assert.Equal("Oslo", destination);
        Assert.Equal("heavy", size); // a positional pattern uses Deconstruct too
    }

    [Fact]
    public void Deconstruct_NegativeNegative_NullObject_ThrowsWhenDeconstructed()
    {
        ParcelModel parcel = null!;

        Assert.Throws<NullReferenceException>(() =>
        {
            var (weight, _) = parcel;
            return weight;
        });
    }
}
