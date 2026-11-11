using PatternMatching.Classifiers;
using PatternMatching.Models;

namespace PatternMatching.Tests;

public class ShippingCostClassifierTest
{
    [Theory]
    [InlineData(0.5, "US", false, 5.00)]
    [InlineData(1, "US", false, 5.00)]     // edge: exactly 1 kg
    [InlineData(5, "US", false, 9.00)]
    [InlineData(2, "US", true, 15.00)]
    [InlineData(2, "DE", true, 40.00)]
    [InlineData(30, "DE", false, 25.00)]   // edge: exactly the 30 kg limit
    public void CalculateCost_PositivePositive_ValidParcel_ReturnsCost(double weight, string country, bool express, double expected)
    {
        var parcel = new ParcelModel((decimal)weight, country, express);

        Assert.Equal((decimal)expected, ShippingCostClassifier.CalculateCost(parcel));
    }

    [Fact]
    public void CalculateCost_PositiveNegative_ParcelOverWeightLimit_IsRefused()
    {
        var parcel = new ParcelModel(30.01m, "US", false);

        Assert.Throws<InvalidOperationException>(() => ShippingCostClassifier.CalculateCost(parcel));
    }

    [Fact]
    public void CalculateCost_NegativeNegative_NullParcel_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => ShippingCostClassifier.CalculateCost(null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void CalculateCost_NegativeNegative_ZeroOrNegativeWeight_IsRejected(double weight)
    {
        var parcel = new ParcelModel((decimal)weight, "US", false);

        Assert.Throws<ArgumentException>(() => ShippingCostClassifier.CalculateCost(parcel));
    }
}
