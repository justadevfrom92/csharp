namespace Banking.Domain;

// The domain only says WHAT it needs. HOW accounts are stored (memory, SQL, files)
// is decided outside the domain by whoever implements this interface.
public interface IBankAccountRepository
{
    BankAccount? FindById(Guid id);
    void Save(BankAccount account);
}

// Whoever wants to react to events (send email, write audit log...) implements this.
public interface IDomainEventDispatcher
{
    void Dispatch(IDomainEvent domainEvent);
}
