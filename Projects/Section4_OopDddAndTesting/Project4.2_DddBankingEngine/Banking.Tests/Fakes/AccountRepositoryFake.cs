using Banking.Domain.Aggregates;
using Banking.Domain.Repositories;

namespace Banking.Tests.Fakes;

// Hand-written fakes instead of a mocking library: they are tiny and easy to read.
public class AccountRepositoryFake : IBankAccountRepository
{
    public Dictionary<Guid, BankAccountAggregate> SavedAccounts { get; } = new();

    public BankAccountAggregate? FindById(Guid id)
    {
        return SavedAccounts.GetValueOrDefault(id);
    }

    public void Save(BankAccountAggregate account)
    {
        SavedAccounts[account.Id] = account;
    }
}
