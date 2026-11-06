using Banking.Domain.ValueObjects;

namespace Banking.Domain.Exceptions;

// Custom exceptions so callers can catch exactly the business problem they care about.
public class InsufficientFundsException : Exception
{
    public InsufficientFundsException(Money balance, Money requested)
        : base($"Cannot withdraw {requested}. Balance is only {balance}.")
    {
        Balance = balance;
        Requested = requested;
    }

    public Money Balance { get; }
    public Money Requested { get; }
}
