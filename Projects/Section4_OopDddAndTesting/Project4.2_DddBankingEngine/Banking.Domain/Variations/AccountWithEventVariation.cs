using Banking.Domain.Events;
using Banking.Domain.ValueObjects;

namespace Banking.Domain.Variations;

// Domain events as a C# "event" that fires IMMEDIATELY, instead of a PendingEvents list sent AFTER saving.
// The difference matters: listeners (send an email, update a report) run BEFORE the account is saved.
// If saving then fails, the email has already gone out for money that was never deposited.
// The original BankAccountAggregate collects events, and BankAccountService sends them only after Save().
public class AccountWithEventVariation
{
    public AccountWithEventVariation(string currency)
    {
        Balance = MoneyValueObject.Zero(currency);
    }

    public event Action<IDomainEvent>? EventRaised;

    public MoneyValueObject Balance { get; private set; }

    public void Deposit(MoneyValueObject amount)
    {
        Balance = Balance.Add(amount);
        EventRaised?.Invoke(new MoneyDepositedEvent(Guid.Empty, amount, Balance, DateTimeOffset.UtcNow)); // runs listeners NOW
    }
}
