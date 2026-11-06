using Banking.Domain.Events;
using Banking.Domain.Services;
using Banking.Domain.ValueObjects;
using Banking.Tests.Fakes;

namespace Banking.Tests;

public class BankAccountServiceTests
{
    [Fact]
    public void Service_saves_account_and_sends_each_event_once()
    {
        var repository = new FakeAccountRepository();
        var dispatcher = new FakeEventDispatcher();
        var service = new BankAccountService(repository, dispatcher);

        Guid accountId = service.OpenAccount("Bob", "USD");
        service.Deposit(accountId, new Money(20, "USD"));
        service.Withdraw(accountId, new Money(5, "USD"));

        Assert.Equal(new Money(15, "USD"), service.GetBalance(accountId));
        Assert.Collection(dispatcher.SentEvents,
            first => Assert.IsType<AccountOpenedEvent>(first),
            second => Assert.IsType<MoneyDepositedEvent>(second),
            third => Assert.IsType<MoneyWithdrawnEvent>(third));
        Assert.Empty(repository.SavedAccounts[accountId].PendingEvents);
    }

    [Fact]
    public void Unknown_account_throws()
    {
        var service = new BankAccountService(new FakeAccountRepository(), new FakeEventDispatcher());

        Assert.Throws<KeyNotFoundException>(() => service.GetBalance(Guid.NewGuid()));
    }
}
