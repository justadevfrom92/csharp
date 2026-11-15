using PatternMatching.Models;

namespace PatternMatching.Variations;

// "Is this a card payment, and how much?" - the type check written four ways.
// Original: PaymentClassifier uses a switch expression with TYPE patterns (CardPaymentModel card => ...).
public static class PaymentTypeCheckVariation
{
    // "is" with a declaration pattern: checks the type AND gives a typed variable in one step. Modern, preferred.
    public static decimal? CardAmountWithIsPattern(object? payment)
    {
        if (payment is CardPaymentModel card)
        {
            return card.Amount;
        }
        return null;
    }

    // "as" + null check: the older way. Same result. Only works for classes/records (as returns null on failure).
    public static decimal? CardAmountWithAs(object? payment)
    {
        var card = payment as CardPaymentModel;
        if (card != null)
        {
            return card.Amount;
        }
        return null;
    }

    // Cast inside try/catch: same result, but uses an EXCEPTION for a normal "no" - slow and noisy. Don't.
    public static decimal? CardAmountWithCast(object? payment)
    {
        try
        {
            return ((CardPaymentModel)payment!).Amount;
        }
        catch (Exception exception) when (exception is InvalidCastException or NullReferenceException)
        {
            return null;
        }
    }

    // GetType() == typeof(...): DIFFERENT behaviour - EXACT type only. A GiftCardPaymentVariation (a child of
    // CardPaymentModel) is NOT matched, while all three versions above DO match it.
    public static decimal? CardAmountWithExactType(object? payment)
    {
        if (payment != null && payment.GetType() == typeof(CardPaymentModel))
        {
            return ((CardPaymentModel)payment).Amount;
        }
        return null;
    }
}
