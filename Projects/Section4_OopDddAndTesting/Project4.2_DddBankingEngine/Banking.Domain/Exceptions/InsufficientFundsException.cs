using Banking.Domain.ValueObjects;

namespace Banking.Domain.Exceptions;

// Custom exceptions so callers can catch exactly the business problem they care about.
public class InsufficientFundsException : Exception
{
    public InsufficientFundsException(MoneyValueObject balance, MoneyValueObject requested)
        : base($"Cannot withdraw {requested}. Balance is only {balance}.")
    {
        Balance = balance;
        Requested = requested;
    }

    public MoneyValueObject Balance { get; }
    public MoneyValueObject Requested { get; }
}
