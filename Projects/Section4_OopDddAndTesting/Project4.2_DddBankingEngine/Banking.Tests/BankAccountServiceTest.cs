using Banking.Domain.Events;
using Banking.Domain.Exceptions;
using Banking.Domain.Services;
using Banking.Domain.ValueObjects;
using Banking.Tests.Fakes;

namespace Banking.Tests;

// Every behaviour has three kinds of test:
//   PositivePositive - VALID input            -> succeeds, account saved, events sent once
//   PositiveNegative - well-formed input that breaks a business rule -> refused, nothing saved or sent
//   NegativeNegative - MALFORMED or missing input (unknown id, null)   -> rejected cleanly
public class BankAccountServiceTest
{
    private readonly AccountRepositoryFake repository = new();
    private readonly EventDispatcherFake dispatcher = new();
    private readonly BankAccountService service;

    public BankAccountServiceTest()
    {
        service = new BankAccountService(repository, dispatcher);
    }

    private static MoneyValueObject Dollars(decimal amount)
    {
        return new MoneyValueObject(amount, "USD");
    }

    // ---------- OpenAccount ----------

    [Fact]
    public void OpenAccount_PositivePositive_ValidOwner_SavesAccountAndSendsOpenedEventOnce()
    {
        Guid accountId = service.OpenAccount("Bob", "USD");

        Assert.True(repository.SavedAccounts.ContainsKey(accountId));
        Assert.IsType<AccountOpenedEvent>(Assert.Single(dispatcher.SentEvents));
        Assert.Empty(repository.SavedAccounts[accountId].PendingEvents);
    }

    [Fact]
    public void OpenAccount_PositiveNegative_WhitespaceOwner_IsRefusedAndNothingSaved()
    {
        Assert.Throws<ArgumentException>(() => service.OpenAccount("   ", "USD"));

        Assert.Empty(repository.SavedAccounts);
        Assert.Empty(dispatcher.SentEvents);
    }

    [Fact]
    public void OpenAccount_NegativeNegative_MalformedCurrency_IsRejectedAndNothingSaved()
    {
        Assert.Throws<ArgumentException>(() => service.OpenAccount("Bob", "X"));

        Assert.Empty(repository.SavedAccounts);
        Assert.Empty(dispatcher.SentEvents);
    }

    // ---------- Deposit ----------

    [Fact]
    public void Deposit_PositivePositive_ValidAmount_UpdatesBalanceAndSendsEvent()
    {
        Guid accountId = service.OpenAccount("Bob", "USD");

        service.Deposit(accountId, Dollars(20));

        Assert.Equal(Dollars(20), service.GetBalance(accountId));
        Assert.IsType<MoneyDepositedEvent>(dispatcher.SentEvents.Last());
    }

    [Fact]
    public void Deposit_PositiveNegative_WrongCurrency_IsRefusedAndNoEventSent()
    {
        Guid accountId = service.OpenAccount("Bob", "USD");
        int eventsBefore = dispatcher.SentEvents.Count;

        Assert.Throws<InvalidOperationException>(() => service.Deposit(accountId, new MoneyValueObject(20, "EUR")));

        Assert.Equal(Dollars(0), service.GetBalance(accountId));
        Assert.Equal(eventsBefore, dispatcher.SentEvents.Count);
    }

    [Fact]
    public void Deposit_NegativeNegative_UnknownAccountId_IsRejected()
    {
        Assert.Throws<KeyNotFoundException>(() => service.Deposit(Guid.NewGuid(), Dollars(20)));
        Assert.Empty(dispatcher.SentEvents);
    }

    // ---------- Withdraw ----------

    [Fact]
    public void Withdraw_PositivePositive_ValidAmount_UpdatesBalanceAndSendsEventsInOrder()
    {
        Guid accountId = service.OpenAccount("Bob", "USD");
        service.Deposit(accountId, Dollars(20));

        service.Withdraw(accountId, Dollars(5));

        Assert.Equal(Dollars(15), service.GetBalance(accountId));
        Assert.Collection(dispatcher.SentEvents,
            first => Assert.IsType<AccountOpenedEvent>(first),
            second => Assert.IsType<MoneyDepositedEvent>(second),
            third => Assert.IsType<MoneyWithdrawnEvent>(third));
    }

    [Fact]
    public void Withdraw_PositiveNegative_MoreThanBalance_IsRefusedAndNoEventSent()
    {
        Guid accountId = service.OpenAccount("Bob", "USD");
        service.Deposit(accountId, Dollars(20));
        int eventsBefore = dispatcher.SentEvents.Count;

        Assert.Throws<InsufficientFundsException>(() => service.Withdraw(accountId, Dollars(500)));

        Assert.Equal(Dollars(20), service.GetBalance(accountId));
        Assert.Equal(eventsBefore, dispatcher.SentEvents.Count);
    }

    [Fact]
    public void Withdraw_NegativeNegative_UnknownAccountId_IsRejected()
    {
        Assert.Throws<KeyNotFoundException>(() => service.Withdraw(Guid.NewGuid(), Dollars(5)));
    }

    // ---------- GetBalance ----------

    [Fact]
    public void GetBalance_PositivePositive_AfterDeposit_ReturnsBalance()
    {
        Guid accountId = service.OpenAccount("Bob", "USD");
        service.Deposit(accountId, Dollars(42));

        Assert.Equal(Dollars(42), service.GetBalance(accountId));
    }

    [Fact]
    public void GetBalance_PositiveNegative_NewAccount_ReturnsZero()
    {
        Guid accountId = service.OpenAccount("Bob", "USD");

        Assert.Equal(Dollars(0), service.GetBalance(accountId));
    }

    [Fact]
    public void GetBalance_NegativeNegative_UnknownAccountId_IsRejected()
    {
        Assert.Throws<KeyNotFoundException>(() => service.GetBalance(Guid.NewGuid()));
    }
}
