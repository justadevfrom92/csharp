using Banking.Domain.ValueObjects;

namespace Banking.Tests;

public class MoneyTest
{
    [Fact]
    public void Two_money_values_with_same_amount_and_currency_are_equal()
    {
        Assert.Equal(new MoneyValueObject(10, "USD"), new MoneyValueObject(10.00m, "usd"));
    }

    [Fact]
    public void Add_returns_new_money_and_leaves_original_unchanged()
    {
        var original = new MoneyValueObject(10, "USD");

        MoneyValueObject result = original.Add(new MoneyValueObject(5, "USD"));

        Assert.Equal(15, result.Amount);
        Assert.Equal(10, original.Amount);
    }

    [Fact]
    public void Adding_different_currencies_throws()
    {
        var dollars = new MoneyValueObject(10, "USD");
        var euros = new MoneyValueObject(10, "EUR");

        Assert.Throws<InvalidOperationException>(() => dollars.Add(euros));
    }

    [Theory]
    [InlineData(-1, "USD")]
    [InlineData(10, "US")]
    [InlineData(10, "")]
    public void Invalid_money_cannot_be_created(decimal amount, string currency)
    {
        Assert.Throws<ArgumentException>(() => new MoneyValueObject(amount, currency));
    }
}
