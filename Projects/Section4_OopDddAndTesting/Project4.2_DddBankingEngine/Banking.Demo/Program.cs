// This whole app is ONE file. The program runs from the top.
// The classes it uses (InMemoryAccountRepository, ConsoleEventDispatcher) are defined at the BOTTOM of this file.
// (In C#, top-level code must come before any class definitions in the file.)

using Banking.Domain.Aggregates;
using Banking.Domain.Dispatchers;
using Banking.Domain.Events;
using Banking.Domain.Exceptions;
using Banking.Domain.Repositories;
using Banking.Domain.Services;
using Banking.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

// ---------- Dependency Injection setup ----------
var services = new ServiceCollection();
services.AddSingleton<IBankAccountRepository, InMemoryAccountRepository>(); // one shared store for the whole app
services.AddSingleton<IDomainEventDispatcher, ConsoleEventDispatcher>();
services.AddTransient<BankAccountService>();                               // new one each time, it holds no data
ServiceProvider provider = services.BuildServiceProvider();

BankAccountService bank = provider.GetRequiredService<BankAccountService>();

Guid accountId = bank.OpenAccount("Alice", "USD");
bank.Deposit(accountId, new MoneyValueObject(100, "USD"));
bank.Withdraw(accountId, new MoneyValueObject(30, "USD"));

try
{
    bank.Withdraw(accountId, new MoneyValueObject(500, "USD"));
}
catch (InsufficientFundsException exception) when (exception.Requested.Amount > 100) // exception filter
{
    Console.WriteLine($"Big withdrawal refused: {exception.Message}");
}
finally
{
    Console.WriteLine($"Final balance: {bank.GetBalance(accountId)}");
}

// ---------- Simple implementations living OUTSIDE the domain ----------
class InMemoryAccountRepository : IBankAccountRepository
{
    private readonly Dictionary<Guid, BankAccountAggregate> accounts = new();

    public BankAccountAggregate? FindById(Guid id)
    {
        return accounts.GetValueOrDefault(id);
    }

    public void Save(BankAccountAggregate account)
    {
        accounts[account.Id] = account;
    }
}

class ConsoleEventDispatcher : IDomainEventDispatcher
{
    public void Dispatch(IDomainEvent domainEvent)
    {
        Console.WriteLine($"[event] {domainEvent}");
    }
}
