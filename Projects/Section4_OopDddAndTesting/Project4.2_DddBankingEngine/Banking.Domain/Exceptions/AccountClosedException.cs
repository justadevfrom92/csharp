namespace Banking.Domain.Exceptions;

public class AccountClosedException : Exception
{
    public AccountClosedException(Guid accountId)
        : base($"Account {accountId} is closed.")
    {
    }
}
