using OperatorsAndLambdas.ValueObjects;

namespace OperatorsAndLambdas.Tests;

// PositivePositive = valid input -> works; PositiveNegative = well-formed input that breaks a rule -> refused;
// NegativeNegative = broken or missing input -> rejected.
public class MoneyValueObjectTest
{
    private static MoneyValueObject Usd(decimal amount)
    {
        return new MoneyValueObject(amount, "USD");
    }

    // ---------- + and - ----------

    [Fact]
    public void Add_PositivePositive_SameCurrency_AddsAmounts()
    {
        Assert.Equal(Usd(7.50m), Usd(5m) + Usd(2.50m));
        Assert.Equal(Usd(2.50m), Usd(5m) - Usd(2.50m));
    }

    [Fact]
    public void Add_PositiveNegative_DifferentCurrencies_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Usd(5m) + new MoneyValueObject(1m, "EUR"));
        Assert.Throws<InvalidOperationException>(() => Usd(5m) - new MoneyValueObject(1m, "EUR"));
    }

    [Fact]
    public void Add_NegativeNegative_NullSide_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => Usd(5m) + null!);
        Assert.Throws<ArgumentNullException>(() => null! - Usd(5m));
    }

    // ---------- * and unary - ----------

    [Fact]
    public void Multiply_PositivePositive_BothOrders_GiveSameResult()
    {
        Assert.Equal(Usd(59.97m), Usd(19.99m) * 3);
        Assert.Equal(Usd(59.97m), 3 * Usd(19.99m));
        Assert.Equal(Usd(-19.99m), -Usd(19.99m));
    }

    [Fact]
    public void Multiply_PositiveNegative_ResultIsRoundedToCents()
    {
        Assert.Equal(Usd(3.33m), Usd(10m) * (1m / 3m));
    }

    [Fact]
    public void Multiply_NegativeNegative_NullMoney_ThrowsArgumentNull()
    {
        MoneyValueObject? nothing = null;
        Assert.Throws<ArgumentNullException>(() => nothing! * 2);
        Assert.Throws<ArgumentNullException>(() => -nothing!);
    }

    // ---------- comparison and equality ----------

    [Fact]
    public void Compare_PositivePositive_OperatorsAgreeWithAmounts()
    {
        Assert.True(Usd(10m) > Usd(5m));
        Assert.True(Usd(5m) < Usd(10m));
        Assert.True(Usd(5m) <= Usd(5m));
        Assert.True(Usd(5m) >= Usd(5m));
        Assert.True(Usd(5m) == Usd(5.00m));
        Assert.True(Usd(5m) != Usd(6m));
    }

    [Fact]
    public void Compare_PositiveNegative_DifferentCurrencies_ThrowsButEqualityIsJustFalse()
    {
        var euros = new MoneyValueObject(5m, "EUR");

        Assert.Throws<InvalidOperationException>(() => Usd(5m) > euros);
        Assert.False(Usd(5m) == euros); // == never throws: different currency = simply not equal
    }

    [Fact]
    public void Compare_NegativeNegative_NullSide_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => Usd(5m) < null!);
        Assert.False(Usd(5m) == null);
    }

    // ---------- constructor and explicit conversion ----------

    [Fact]
    public void Create_PositivePositive_LowerCaseCurrency_IsStoredUpperCaseAndConverts()
    {
        var money = new MoneyValueObject(12.345m, "usd");

        Assert.Equal("USD", money.Currency);
        Assert.Equal(12.34m, (decimal)money); // rounded to cents (banker's rounding: .345 -> .34)
    }

    [Theory]
    [InlineData("US")]
    [InlineData("DOLLARS")]
    public void Create_PositiveNegative_CurrencyNotThreeLetters_Throws(string currency)
    {
        Assert.Throws<ArgumentException>(() => new MoneyValueObject(1m, currency));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NegativeNegative_MissingCurrency_Throws(string? currency)
    {
        Assert.Throws<ArgumentException>(() => new MoneyValueObject(1m, currency!));
    }
}
