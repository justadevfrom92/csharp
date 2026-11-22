using Tuples.Calculators;

namespace Tuples.Tests;

public class PriceStatsCalculatorTest
{
    [Fact]
    public void Summarize_PositivePositive_NamedElementsAndDeconstruction()
    {
        var result = PriceStatsCalculator.Summarize([19.99m, 5.01m, 25.00m]);
        var (total, count, average) = PriceStatsCalculator.Summarize([19.99m, 5.01m, 25.00m]);

        Assert.Equal(50.00m, result.Total);
        Assert.Equal(3, result.Count);
        Assert.Equal(16.67m, result.Average);
        Assert.Equal((50.00m, 3, 16.67m), (total, count, average));
    }

    [Fact]
    public void Summarize_PositiveNegative_EmptyList_IsAllZeros_RangeIsNull()
    {
        Assert.Equal((0m, 0, 0m), PriceStatsCalculator.Summarize([]));
        Assert.Null(PriceStatsCalculator.Range([]));
        Assert.Equal((5.01m, 25.00m), PriceStatsCalculator.Range([19.99m, 5.01m, 25.00m]));
    }

    [Fact]
    public void Summarize_NegativeNegative_NullList_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PriceStatsCalculator.Summarize(null!));
        Assert.Throws<ArgumentNullException>(() => PriceStatsCalculator.Range(null!));
    }
}
