namespace Banking.Domain;

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

public class AccountClosedException : Exception
{
    public AccountClosedException(Guid accountId)
        : base($"Account {accountId} is closed.")
    {
    }
}
