using Banking.Domain.Aggregates;
using Banking.Domain.Events;
using Banking.Domain.Exceptions;
using Banking.Domain.ValueObjects;
using Banking.Domain.Variations;

namespace Banking.Tests;

// Each variation compared with the original. PositivePositive = valid input -> same result;
// PositiveNegative = valid input where the variation behaves DIFFERENTLY (the point of the comparison);
// NegativeNegative = broken input -> rejected.
public class VariationsTest
{
    private static MoneyValueObject Dollars(decimal amount)
    {
        return new MoneyValueObject(amount, "USD");
    }

    private static BankAccountAggregate AccountWith(decimal balance)
    {
        BankAccountAggregate account = BankAccountAggregate.Open("Ann", "USD");
        account.Deposit(Dollars(balance));
        return account;
    }

    // ---------- money as record / positional record / struct / class ----------

    [Fact]
    public void Money_PositivePositive_RecordStructAndPositional_AreEqualByValue()
    {
        Assert.Equal(Dollars(5), Dollars(5));
        Assert.Equal(new MoneyStructVariation(5, "USD"), new MoneyStructVariation(5, "USD"));
        Assert.Equal(new MoneyPositionalVariation(5, "USD"), new MoneyPositionalVariation(5, "USD"));
    }

    [Fact]
    public void Money_PositiveNegative_EachVariationHasAHole_TheOriginalDoesNot()
    {
        var positional = new MoneyPositionalVariation(5, "USD");
        MoneyPositionalVariation negative = positional with { Amount = -5 };     // "with" skips the check
        MoneyStructVariation zeroed = default;                                 // skips the constructor
        var classA = new MoneyClassVariation(5, "USD");
        var classB = new MoneyClassVariation(5, "USD");

        Assert.Equal(-5, negative.Amount);
        Assert.Null(zeroed.Currency);
        Assert.False(classA == classB);       // reference equality: two objects, so "not equal"
        Assert.False(classA.Equals(classB));
        // The original can't do any of these: get-only properties (no "with"), it's a class (no default), it's a record (value equality).
    }

    [Fact]
    public void Money_NegativeNegative_NegativeAmountInConstructor_EveryVersionRejects()
    {
        Assert.Throws<ArgumentException>(() => Dollars(-1));
        Assert.Throws<ArgumentException>(() => new MoneyPositionalVariation(-1, "USD"));
        Assert.Throws<ArgumentException>(() => new MoneyStructVariation(-1, "USD"));
        Assert.Throws<ArgumentException>(() => new MoneyClassVariation(-1, "USD"));
    }

    // ---------- withdraw: exception / try / result ----------

    [Fact]
    public void Withdraw_PositivePositive_EnoughMoney_AllThreeStylesWithdraw()
    {
        BankAccountAggregate first = AccountWith(100);
        BankAccountAggregate second = AccountWith(100);
        BankAccountAggregate third = AccountWith(100);

        WithdrawStyleVariation.WithException(first, Dollars(30));
        bool tried = WithdrawStyleVariation.TryWithdraw(second, Dollars(30), out string? error);
        WithdrawResultVariation result = WithdrawStyleVariation.WithResult(third, Dollars(30));

        Assert.Equal(Dollars(70), first.Balance);
        Assert.True(tried);
        Assert.Null(error);
        Assert.Equal(Dollars(70), second.Balance);
        Assert.True(result.Succeeded);
        Assert.Equal(Dollars(70), third.Balance);
    }

    [Fact]
    public void Withdraw_PositiveNegative_NotEnoughMoney_ThrowVersusFalseVersusFailedResult()
    {
        BankAccountAggregate account = AccountWith(10);

        Assert.Throws<InsufficientFundsException>(() => WithdrawStyleVariation.WithException(account, Dollars(50)));
        Assert.False(WithdrawStyleVariation.TryWithdraw(account, Dollars(50), out string? error));
        WithdrawResultVariation result = WithdrawStyleVariation.WithResult(account, Dollars(50));

        Assert.Equal("Balance is only 10.00 USD.", error);
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot withdraw 50.00 USD", result.Error);
        Assert.Equal(Dollars(10), account.Balance); // nothing changed in any style
    }

    [Fact]
    public void Withdraw_NegativeNegative_NullAmount_AllStylesThrow()
    {
        BankAccountAggregate account = AccountWith(10);

        Assert.Throws<ArgumentNullException>(() => WithdrawStyleVariation.WithException(account, null!));
        Assert.Throws<ArgumentNullException>(() => WithdrawStyleVariation.TryWithdraw(account, null!, out _));
        Assert.Throws<ArgumentNullException>(() => WithdrawStyleVariation.WithResult(account, null!)); // a Result only covers EXPECTED failures
    }

    // ---------- events: collected (original) vs fired immediately ----------

    [Fact]
    public void Events_PositivePositive_BothKindsReportTheDeposit()
    {
        var immediate = new AccountWithEventVariation("USD");
        var heard = new List<IDomainEvent>();
        immediate.EventRaised += heard.Add;
        BankAccountAggregate collected = BankAccountAggregate.Open("Ann", "USD");

        immediate.Deposit(Dollars(5));
        collected.Deposit(Dollars(5));

        Assert.IsType<MoneyDepositedEvent>(Assert.Single(heard));
        Assert.IsType<MoneyDepositedEvent>(collected.PendingEvents[^1]);
    }

    [Fact]
    public void Events_PositiveNegative_ImmediateEventIsAlreadyHandled_BeforeAnythingIsSaved()
    {
        var immediate = new AccountWithEventVariation("USD");
        int emailsSent = 0;
        immediate.EventRaised += _ => emailsSent++;
        BankAccountAggregate collected = BankAccountAggregate.Open("Ann", "USD");
        collected.ClearPendingEvents();

        immediate.Deposit(Dollars(5));
        collected.Deposit(Dollars(5));
        collected.ClearPendingEvents(); // pretend saving FAILED: the original simply never sends them

        Assert.Equal(1, emailsSent);             // too late to take back
        Assert.Empty(collected.PendingEvents);
    }

    [Fact]
    public void Events_NegativeNegative_NoListeners_OrNullAmount()
    {
        var immediate = new AccountWithEventVariation("USD");

        immediate.Deposit(Dollars(5));           // nobody subscribed: "?.Invoke" does nothing, no crash

        Assert.Equal(Dollars(5), immediate.Balance);
        Assert.Throws<ArgumentNullException>(() => immediate.Deposit(null!));
    }
}
