using Banking.Domain;
using Microsoft.Extensions.DependencyInjection;

// ---------- Dependency Injection setup ----------
var services = new ServiceCollection();
services.AddSingleton<IBankAccountRepository, InMemoryAccountRepository>(); // one shared store for the whole app
services.AddSingleton<IDomainEventDispatcher, ConsoleEventDispatcher>();
services.AddTransient<BankAccountService>();                               // new one each time, it holds no data
ServiceProvider provider = services.BuildServiceProvider();

BankAccountService bank = provider.GetRequiredService<BankAccountService>();

Guid accountId = bank.OpenAccount("Alice", "USD");
bank.Deposit(accountId, new Money(100, "USD"));
bank.Withdraw(accountId, new Money(30, "USD"));

try
{
    bank.Withdraw(accountId, new Money(500, "USD"));
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
    private readonly Dictionary<Guid, BankAccount> accounts = new();

    public BankAccount? FindById(Guid id)
    {
        return accounts.GetValueOrDefault(id);
    }

    public void Save(BankAccount account)
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
