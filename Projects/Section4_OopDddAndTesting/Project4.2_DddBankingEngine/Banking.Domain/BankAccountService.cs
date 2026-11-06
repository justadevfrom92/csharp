namespace Banking.Domain;

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
        BankAccount account = BankAccount.Open(ownerName, currency);
        SaveAndDispatchEvents(account);
        return account.Id;
    }

    public void Deposit(Guid accountId, Money amount)
    {
        BankAccount account = LoadAccount(accountId);
        account.Deposit(amount);
        SaveAndDispatchEvents(account);
    }

    public void Withdraw(Guid accountId, Money amount)
    {
        BankAccount account = LoadAccount(accountId);
        account.Withdraw(amount);
        SaveAndDispatchEvents(account);
    }

    public Money GetBalance(Guid accountId)
    {
        return LoadAccount(accountId).Balance;
    }

    private BankAccount LoadAccount(Guid accountId)
    {
        BankAccount? account = repository.FindById(accountId);
        if (account == null)
        {
            throw new KeyNotFoundException($"Account {accountId} not found.");
        }
        return account;
    }

    private void SaveAndDispatchEvents(BankAccount account)
    {
        repository.Save(account);
        foreach (IDomainEvent domainEvent in account.PendingEvents)
        {
            eventDispatcher.Dispatch(domainEvent);
        }
        account.ClearPendingEvents();
    }
}
