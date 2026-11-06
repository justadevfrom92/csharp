using Banking.Domain.ValueObjects;

namespace Banking.Tests;

// Every behaviour has three kinds of test:
//   PositivePositive - VALID input            -> succeeds
//   PositiveNegative - well-formed input that breaks a business rule -> correctly refused / negative result
//   NegativeNegative - MALFORMED or missing input (null, bad format)  -> rejected cleanly
public class MoneyTest
{
    // ---------- Creating money ----------

    [Fact]
    public void Create_PositivePositive_ValidAmountAndCurrency_IsCreatedAndNormalised()
    {
        var money = new MoneyValueObject(10.456m, "usd");

        Assert.Equal(10.46m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Create_PositiveNegative_NegativeAmount_IsRefused()
    {
        // -1 is a perfectly good decimal, but money can't be negative.
        Assert.Throws<ArgumentException>(() => new MoneyValueObject(-1, "USD"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("DOLLARS")]
    [InlineData(null)]
    public void Create_NegativeNegative_MalformedCurrency_IsRejected(string? currency)
    {
        Assert.Throws<ArgumentException>(() => new MoneyValueObject(10, currency!));
    }

    // ---------- Equality ----------

    [Fact]
    public void Equals_PositivePositive_SameAmountAndCurrency_AreEqual()
    {
        Assert.Equal(new MoneyValueObject(10, "USD"), new MoneyValueObject(10.00m, "usd"));
    }

    [Fact]
    public void Equals_PositiveNegative_DifferentAmounts_AreNotEqual()
    {
        Assert.NotEqual(new MoneyValueObject(10, "USD"), new MoneyValueObject(11, "USD"));
    }

    [Fact]
    public void Equals_NegativeNegative_ComparedWithNull_IsNotEqual()
    {
        Assert.False(new MoneyValueObject(10, "USD").Equals(null));
    }

    // ---------- Add ----------

    [Fact]
    public void Add_PositivePositive_SameCurrency_ReturnsNewSumAndLeavesOriginalUnchanged()
    {
        var original = new MoneyValueObject(10, "USD");

        MoneyValueObject result = original.Add(new MoneyValueObject(5, "USD"));

        Assert.Equal(15, result.Amount);
        Assert.Equal(10, original.Amount);
    }

    [Fact]
    public void Add_PositiveNegative_DifferentCurrency_IsRefused()
    {
        var dollars = new MoneyValueObject(10, "USD");
        var euros = new MoneyValueObject(10, "EUR");

        Assert.Throws<InvalidOperationException>(() => dollars.Add(euros));
    }

    [Fact]
    public void Add_NegativeNegative_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MoneyValueObject(10, "USD").Add(null!));
    }

    // ---------- Subtract ----------

    [Fact]
    public void Subtract_PositivePositive_SmallerAmount_ReturnsDifference()
    {
        MoneyValueObject result = new MoneyValueObject(10, "USD").Subtract(new MoneyValueObject(4, "USD"));

        Assert.Equal(6, result.Amount);
    }

    [Fact]
    public void Subtract_PositiveNegative_LargerAmount_IsRefusedBecauseResultWouldBeNegative()
    {
        var ten = new MoneyValueObject(10, "USD");

        Assert.Throws<ArgumentException>(() => ten.Subtract(new MoneyValueObject(15, "USD")));
    }

    [Fact]
    public void Subtract_NegativeNegative_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MoneyValueObject(10, "USD").Subtract(null!));
    }

    // ---------- IsLessThan ----------

    [Fact]
    public void IsLessThan_PositivePositive_SmallerAmount_ReturnsTrue()
    {
        Assert.True(new MoneyValueObject(5, "USD").IsLessThan(new MoneyValueObject(10, "USD")));
    }

    [Fact]
    public void IsLessThan_PositiveNegative_LargerAmount_ReturnsFalse()
    {
        Assert.False(new MoneyValueObject(10, "USD").IsLessThan(new MoneyValueObject(5, "USD")));
    }

    [Fact]
    public void IsLessThan_NegativeNegative_Null_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MoneyValueObject(10, "USD").IsLessThan(null!));
    }
}
