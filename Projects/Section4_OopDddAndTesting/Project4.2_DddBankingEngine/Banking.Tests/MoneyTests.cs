using Banking.Domain.ValueObjects;

namespace Banking.Tests;

public class MoneyTests
{
    [Fact]
    public void Two_money_values_with_same_amount_and_currency_are_equal()
    {
        Assert.Equal(new Money(10, "USD"), new Money(10.00m, "usd"));
    }

    [Fact]
    public void Add_returns_new_money_and_leaves_original_unchanged()
    {
        var original = new Money(10, "USD");

        Money result = original.Add(new Money(5, "USD"));

        Assert.Equal(15, result.Amount);
        Assert.Equal(10, original.Amount);
    }

    [Fact]
    public void Adding_different_currencies_throws()
    {
        var dollars = new Money(10, "USD");
        var euros = new Money(10, "EUR");

        Assert.Throws<InvalidOperationException>(() => dollars.Add(euros));
    }

    [Theory]
    [InlineData(-1, "USD")]
    [InlineData(10, "US")]
    [InlineData(10, "")]
    public void Invalid_money_cannot_be_created(decimal amount, string currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(amount, currency));
    }
}
