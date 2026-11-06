using Banking.Domain.Aggregates;
using Banking.Domain.Events;
using Banking.Domain.Exceptions;
using Banking.Domain.ValueObjects;

namespace Banking.Tests;

public class BankAccountTest
{
    private static MoneyValueObject Dollars(decimal amount)
    {
        return new MoneyValueObject(amount, "USD");
    }

    [Fact]
    public void New_account_has_zero_balance_and_an_AccountOpened_event()
    {
        BankAccountAggregate account = BankAccountAggregate.Open("Alice", "USD");

        Assert.Equal(Dollars(0), account.Balance);
        Assert.IsType<AccountOpenedEvent>(Assert.Single(account.PendingEvents));
    }

    [Fact]
    public void Deposit_increases_balance_and_raises_event()
    {
        BankAccountAggregate account = BankAccountAggregate.Open("Alice", "USD");

        account.Deposit(Dollars(100));

        Assert.Equal(Dollars(100), account.Balance);
        MoneyDepositedEvent depositEvent = Assert.IsType<MoneyDepositedEvent>(account.PendingEvents.Last());
        Assert.Equal(Dollars(100), depositEvent.NewBalance);
    }

    [Fact]
    public void Withdrawing_more_than_balance_throws_and_balance_is_unchanged()
    {
        BankAccountAggregate account = BankAccountAggregate.Open("Alice", "USD");
        account.Deposit(Dollars(50));

        var exception = Assert.Throws<InsufficientFundsException>(() => account.Withdraw(Dollars(80)));

        Assert.Equal(Dollars(80), exception.Requested);
        Assert.Equal(Dollars(50), account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.001)] // rounds to 0.00
    public void Deposit_of_zero_is_rejected(decimal amount)
    {
        BankAccountAggregate account = BankAccountAggregate.Open("Alice", "USD");

        Assert.Throws<ArgumentException>(() => account.Deposit(Dollars(amount)));
    }

    [Fact]
    public void Deposit_in_wrong_currency_is_rejected()
    {
        BankAccountAggregate account = BankAccountAggregate.Open("Alice", "USD");

        Assert.Throws<InvalidOperationException>(() => account.Deposit(new MoneyValueObject(10, "EUR")));
    }

    [Fact]
    public void Account_with_money_cannot_be_closed()
    {
        BankAccountAggregate account = BankAccountAggregate.Open("Alice", "USD");
        account.Deposit(Dollars(1));

        Assert.Throws<InvalidOperationException>(() => account.Close());
    }

    [Fact]
    public void Closed_account_rejects_deposits()
    {
        BankAccountAggregate account = BankAccountAggregate.Open("Alice", "USD");
        account.Close();

        Assert.Throws<AccountClosedException>(() => account.Deposit(Dollars(10)));
    }
}
