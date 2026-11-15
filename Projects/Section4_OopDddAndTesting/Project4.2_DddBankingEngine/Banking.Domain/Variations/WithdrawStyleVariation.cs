using Banking.Domain.Aggregates;
using Banking.Domain.Exceptions;
using Banking.Domain.ValueObjects;

namespace Banking.Domain.Variations;

// "Withdraw, but there might not be enough money" - three ways to tell the caller it failed.
// ORIGINAL: BankAccountAggregate.Withdraw THROWS InsufficientFundsException.
public static class WithdrawStyleVariation
{
    // EXCEPTION (original): the caller can't forget to handle it - unhandled, it stops the request.
    // Right when failing is unusual or a programming mistake. Exceptions are slow if they happen all the time.
    public static void WithException(BankAccountAggregate account, MoneyValueObject amount)
    {
        account.Withdraw(amount);
    }

    // TRY pattern: bool + out parameter, like int.TryParse. Cheap, but the caller CAN ignore the bool.
    public static bool TryWithdraw(BankAccountAggregate account, MoneyValueObject amount, out string? error)
    {
        if (account.Balance.IsLessThan(amount))
        {
            error = $"Balance is only {account.Balance}.";
            return false;
        }
        account.Withdraw(amount);
        error = null;
        return true;
    }

    // RESULT object: success/failure is DATA that can be returned, stored or sent as JSON. Popular for
    // expected business failures ("not enough money" is a normal answer, not a bug).
    public static WithdrawResultVariation WithResult(BankAccountAggregate account, MoneyValueObject amount)
    {
        try
        {
            account.Withdraw(amount);
            return new WithdrawResultVariation(true, null);
        }
        catch (InsufficientFundsException exception)
        {
            return new WithdrawResultVariation(false, exception.Message);
        }
    }
}
