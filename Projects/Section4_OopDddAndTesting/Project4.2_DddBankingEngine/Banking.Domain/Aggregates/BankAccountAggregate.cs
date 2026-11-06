using Banking.Domain.Events;
using Banking.Domain.Exceptions;
using Banking.Domain.ValueObjects;

namespace Banking.Domain.Aggregates;

// AGGREGATE ROOT: the only way to change an account is through these methods,
// so the rules (invariants) below can never be skipped:
//   1. Balance never goes below zero.
//   2. All money in the account uses the account's currency.
//   3. A closed account accepts no deposits or withdrawals.
//   4. An account can only be closed when its balance is zero.
public class BankAccountAggregate
{
    private readonly List<IDomainEvent> pendingEvents = new();

    public Guid Id { get; }
    public string OwnerName { get; }
    public MoneyValueObject Balance { get; private set; }
    public bool IsClosed { get; private set; }

    public IReadOnlyList<IDomainEvent> PendingEvents
    {
        get
        {
            return pendingEvents;
        }
    }

    private BankAccountAggregate(Guid id, string ownerName, string currency)
    {
        Id = id;
        OwnerName = ownerName;
        Balance = MoneyValueObject.Zero(currency);
    }

    public static BankAccountAggregate Open(string ownerName, string currency)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
        {
            throw new ArgumentException("Owner name is required.", nameof(ownerName));
        }

        var account = new BankAccountAggregate(Guid.NewGuid(), ownerName.Trim(), currency);
        account.pendingEvents.Add(new AccountOpenedEvent(account.Id, account.OwnerName, DateTimeOffset.UtcNow));
        return account;
    }

    public void Deposit(MoneyValueObject amount)
    {
        CheckIsOpen();
        CheckAmountIsPositive(amount);

        Balance = Balance.Add(amount);
        pendingEvents.Add(new MoneyDepositedEvent(Id, amount, Balance, DateTimeOffset.UtcNow));
    }

    public void Withdraw(MoneyValueObject amount)
    {
        CheckIsOpen();
        CheckAmountIsPositive(amount);
        if (Balance.IsLessThan(amount))
        {
            throw new InsufficientFundsException(Balance, amount);
        }

        Balance = Balance.Subtract(amount);
        pendingEvents.Add(new MoneyWithdrawnEvent(Id, amount, Balance, DateTimeOffset.UtcNow));
    }

    public void Close()
    {
        CheckIsOpen();
        if (Balance.Amount != 0)
        {
            throw new InvalidOperationException("Withdraw all money before closing the account.");
        }

        IsClosed = true;
        pendingEvents.Add(new AccountClosedEvent(Id, DateTimeOffset.UtcNow));
    }

    // Called after the events have been sent, so they are not sent twice.
    public void ClearPendingEvents()
    {
        pendingEvents.Clear();
    }

    private void CheckIsOpen()
    {
        if (IsClosed)
        {
            throw new AccountClosedException(Id);
        }
    }

    private void CheckAmountIsPositive(MoneyValueObject amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
        {
            throw new ArgumentException("Amount must be more than zero.", nameof(amount));
        }
    }
}
