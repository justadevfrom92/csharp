using Banking.Domain.Aggregates;
using Banking.Domain.Events;
using Banking.Domain.Exceptions;
using Banking.Domain.ValueObjects;

namespace Banking.Tests;

// Every behaviour has three kinds of test:
//   PositivePositive - VALID input            -> succeeds
//   PositiveNegative - well-formed input that breaks a business rule -> correctly refused, nothing changes
//   NegativeNegative - MALFORMED or missing input (null, zero, bad format) -> rejected cleanly
public class BankAccountTest
{
    private static MoneyValueObject Dollars(decimal amount)
    {
        return new MoneyValueObject(amount, "USD");
    }

    private static BankAccountAggregate AccountWithBalance(decimal amount)
    {
        BankAccountAggregate account = BankAccountAggregate.Open("Alice", "USD");
        if (amount > 0)
        {
            account.Deposit(Dollars(amount));
        }
        return account;
    }

    // ---------- Open ----------

    [Fact]
    public void Open_PositivePositive_ValidOwnerAndCurrency_HasZeroBalanceAndAccountOpenedEvent()
    {
        BankAccountAggregate account = BankAccountAggregate.Open("  Alice  ", "USD");

        Assert.Equal("Alice", account.OwnerName);
        Assert.Equal(Dollars(0), account.Balance);
        Assert.IsType<AccountOpenedEvent>(Assert.Single(account.PendingEvents));
    }

    [Fact]
    public void Open_PositiveNegative_WhitespaceOwnerName_IsRefused()
    {
        // "   " is a real string, but it isn't a name.
        Assert.Throws<ArgumentException>(() => BankAccountAggregate.Open("   ", "USD"));
    }

    [Theory]
    [InlineData(null, "USD")]
    [InlineData("Alice", "US")]
    [InlineData("Alice", null)]
    public void Open_NegativeNegative_MissingOrMalformedInput_IsRejected(string? owner, string? currency)
    {
        Assert.Throws<ArgumentException>(() => BankAccountAggregate.Open(owner!, currency!));
    }

    // ---------- Deposit ----------

    [Fact]
    public void Deposit_PositivePositive_ValidAmount_IncreasesBalanceAndRaisesEvent()
    {
        BankAccountAggregate account = AccountWithBalance(0);

        account.Deposit(Dollars(100));

        Assert.Equal(Dollars(100), account.Balance);
        MoneyDepositedEvent depositEvent = Assert.IsType<MoneyDepositedEvent>(account.PendingEvents.Last());
        Assert.Equal(Dollars(100), depositEvent.NewBalance);
    }

    [Fact]
    public void Deposit_PositiveNegative_ValidAmountIntoClosedAccount_IsRefused()
    {
        BankAccountAggregate account = AccountWithBalance(0);
        account.Close();

        Assert.Throws<AccountClosedException>(() => account.Deposit(Dollars(10)));
        Assert.Equal(Dollars(0), account.Balance);
    }

    [Fact]
    public void Deposit_PositiveNegative_ValidAmountInWrongCurrency_IsRefused()
    {
        BankAccountAggregate account = AccountWithBalance(50);

        Assert.Throws<InvalidOperationException>(() => account.Deposit(new MoneyValueObject(10, "EUR")));
        Assert.Equal(Dollars(50), account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.001)] // rounds to 0.00
    public void Deposit_NegativeNegative_ZeroAmount_IsRejected(decimal amount)
    {
        BankAccountAggregate account = AccountWithBalance(0);

        Assert.Throws<ArgumentException>(() => account.Deposit(Dollars(amount)));
    }

    [Fact]
    public void Deposit_NegativeNegative_Null_IsRejected()
    {
        BankAccountAggregate account = AccountWithBalance(0);

        Assert.Throws<ArgumentNullException>(() => account.Deposit(null!));
    }

    // ---------- Withdraw ----------

    [Fact]
    public void Withdraw_PositivePositive_ValidAmount_ReducesBalanceAndRaisesEvent()
    {
        BankAccountAggregate account = AccountWithBalance(100);

        account.Withdraw(Dollars(30));

        Assert.Equal(Dollars(70), account.Balance);
        Assert.IsType<MoneyWithdrawnEvent>(account.PendingEvents.Last());
    }

    [Fact]
    public void Withdraw_PositiveNegative_ValidAmountOverBalance_IsRefusedAndBalanceUnchanged()
    {
        BankAccountAggregate account = AccountWithBalance(100);

        var exception = Assert.Throws<InsufficientFundsException>(() => account.Withdraw(Dollars(500)));

        Assert.Equal(Dollars(500), exception.Requested);
        Assert.Equal(Dollars(100), account.Balance);
    }

    [Fact]
    public void Withdraw_NegativeNegative_ZeroAmount_IsRejected()
    {
        BankAccountAggregate account = AccountWithBalance(100);

        Assert.Throws<ArgumentException>(() => account.Withdraw(Dollars(0)));
        Assert.Equal(Dollars(100), account.Balance);
    }

    [Fact]
    public void Withdraw_NegativeNegative_Null_IsRejected()
    {
        BankAccountAggregate account = AccountWithBalance(100);

        Assert.Throws<ArgumentNullException>(() => account.Withdraw(null!));
    }

    // ---------- Close ----------

    [Fact]
    public void Close_PositivePositive_ZeroBalance_ClosesAndRaisesEvent()
    {
        BankAccountAggregate account = AccountWithBalance(0);

        account.Close();

        Assert.True(account.IsClosed);
        Assert.IsType<AccountClosedEvent>(account.PendingEvents.Last());
    }

    [Fact]
    public void Close_PositiveNegative_AccountStillHasMoney_IsRefused()
    {
        BankAccountAggregate account = AccountWithBalance(1);

        Assert.Throws<InvalidOperationException>(() => account.Close());
        Assert.False(account.IsClosed);
    }

    [Fact]
    public void Close_NegativeNegative_AlreadyClosedAccount_IsRejected()
    {
        BankAccountAggregate account = AccountWithBalance(0);
        account.Close();

        Assert.Throws<AccountClosedException>(() => account.Close());
    }
}
