namespace Banking.Domain;

// The domain only says WHAT it needs. HOW accounts are stored (memory, SQL, files)
// is decided outside the domain by whoever implements this interface.
public interface IBankAccountRepository
{
    BankAccount? FindById(Guid id);
    void Save(BankAccount account);
}
