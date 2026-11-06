using Banking.Domain.Aggregates;
using Banking.Domain.Dispatchers;
using Banking.Domain.Events;
using Banking.Domain.Repositories;
using Banking.Domain.ValueObjects;

namespace Banking.Domain.Services;

// Loads an account, runs one action on it, saves it, then sends out its events.
public class BankAccountService
{
    private readonly IBankAccountRepository repository;
    private readonly IDomainEventDispatcher eventDispatcher;

    public BankAccountService(IBankAccountRepository repository, IDomainEventDispatcher eventDispatcher)
    {
        this.repository = repository;
        this.eventDispatcher = eventDispatcher;
    }

    public Guid OpenAccount(string ownerName, string currency)
    {
        BankAccountAggregate account = BankAccountAggregate.Open(ownerName, currency);
        SaveAndDispatchEvents(account);
        return account.Id;
    }

    public void Deposit(Guid accountId, MoneyValueObject amount)
    {
        BankAccountAggregate account = LoadAccount(accountId);
        account.Deposit(amount);
        SaveAndDispatchEvents(account);
    }

    public void Withdraw(Guid accountId, MoneyValueObject amount)
    {
        BankAccountAggregate account = LoadAccount(accountId);
        account.Withdraw(amount);
        SaveAndDispatchEvents(account);
    }

    public MoneyValueObject GetBalance(Guid accountId)
    {
        return LoadAccount(accountId).Balance;
    }

    private BankAccountAggregate LoadAccount(Guid accountId)
    {
        BankAccountAggregate? account = repository.FindById(accountId);
        if (account == null)
        {
            throw new KeyNotFoundException($"Account {accountId} not found.");
        }
        return account;
    }

    private void SaveAndDispatchEvents(BankAccountAggregate account)
    {
        repository.Save(account);
        foreach (IDomainEvent domainEvent in account.PendingEvents)
        {
            eventDispatcher.Dispatch(domainEvent);
        }
        account.ClearPendingEvents();
    }
}
