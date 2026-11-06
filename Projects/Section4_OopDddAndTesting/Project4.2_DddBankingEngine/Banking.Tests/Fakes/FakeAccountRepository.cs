using Banking.Domain.Aggregates;
using Banking.Domain.Repositories;

namespace Banking.Tests.Fakes;

// Hand-written fakes instead of a mocking library: they are tiny and easy to read.
public class FakeAccountRepository : IBankAccountRepository
{
    public Dictionary<Guid, BankAccount> SavedAccounts { get; } = new();

    public BankAccount? FindById(Guid id)
    {
        return SavedAccounts.GetValueOrDefault(id);
    }

    public void Save(BankAccount account)
    {
        SavedAccounts[account.Id] = account;
    }
}
