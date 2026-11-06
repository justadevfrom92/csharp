using Banking.Domain.Aggregates;

namespace Banking.Domain.Repositories;

// The domain only says WHAT it needs. HOW accounts are stored (memory, SQL, files)
// is decided outside the domain by whoever implements this interface.
public interface IBankAccountRepository
{
    BankAccountAggregate? FindById(Guid id);
    void Save(BankAccountAggregate account);
}
