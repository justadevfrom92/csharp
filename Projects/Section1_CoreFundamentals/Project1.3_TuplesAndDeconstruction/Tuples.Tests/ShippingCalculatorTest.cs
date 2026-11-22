using Tuples.Calculators;

namespace Tuples.Tests;

public class ShippingCalculatorTest
{
    [Theory]
    [InlineData("US", false, 5)]
    [InlineData("US", true, 15)]
    [InlineData("CA", false, 12)]
    [InlineData("MX", false, 12)]
    public void Cost_PositivePositive_EachRuleMatches(string country, bool express, decimal expected)
    {
        Assert.Equal(expected, ShippingCalculator.Cost(country, express));
    }

    [Theory]
    [InlineData("us", false, 5)]   // lower case is normalised
    [InlineData("CA", true, 40)]   // no "CA express" rule: falls to (_, true)
    [InlineData("FR", false, 25)]  // unknown country: (_, false)
    public void Cost_PositiveNegative_CaseAndFallThroughRules(string country, bool express, decimal expected)
    {
        Assert.Equal(expected, ShippingCalculator.Cost(country, express));
    }

    [Fact]
    public void Cost_NegativeNegative_BlankCountry_Throws()
    {
        Assert.Throws<ArgumentException>(() => ShippingCalculator.Cost(" ", false));
        Assert.Throws<ArgumentNullException>(() => ShippingCalculator.Cost(null!, false));
    }
}
