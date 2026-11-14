using BehavioralPatterns.Services;
using BehavioralPatterns.Strategies;

namespace BehavioralPatterns.Tests;

// PositivePositive = valid input -> works; PositiveNegative = well-formed input that breaks a rule -> refused;
// NegativeNegative = broken or missing input -> rejected.
public class StrategyTest
{
    private readonly ShippingService service = new ShippingService(
        [new StandardShippingStrategy(), new ExpressShippingStrategy(), new FreeOverThresholdShippingStrategy()]);

    [Theory]
    [InlineData("standard", 60, 2, 6.99)]
    [InlineData("EXPRESS", 60, 2, 19.99)]        // option names ignore case
    [InlineData("free-over-100", 100, 2, 0)]
    [InlineData("free-over-100", 99.99, 2, 6.99)] // just under the threshold -> falls back to standard
    public void Quote_PositivePositive_EachStrategyGivesItsOwnPrice(string option, decimal total, decimal weight, decimal expected)
    {
        Assert.Equal(expected, service.Quote(option, total, weight));
    }

    [Fact]
    public void Quote_PositiveNegative_UnknownOption_ListsTheValidOnes()
    {
        var error = Assert.Throws<KeyNotFoundException>(() => service.Quote("teleport", 60m, 2m));

        Assert.Contains("standard, express, free-over-100", error.Message);
    }

    [Fact]
    public void Quote_NegativeNegative_MissingOptionOrBadNumbers_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => service.Quote("", 60m, 2m));
        Assert.ThrowsAny<ArgumentException>(() => service.Quote(null!, 60m, 2m));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Quote("standard", -1m, 2m));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Quote("standard", 60m, 0m));
        Assert.Throws<ArgumentNullException>(() => new ShippingService(null!));
    }
}
