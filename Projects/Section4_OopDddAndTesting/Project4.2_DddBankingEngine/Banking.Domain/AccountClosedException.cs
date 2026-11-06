namespace Banking.Domain;

public class AccountClosedException : Exception
{
    public AccountClosedException(Guid accountId)
        : base($"Account {accountId} is closed.")
    {
    }
}
