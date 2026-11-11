using PatternMatching.Models;

namespace PatternMatching.Classifiers;

public static class PaymentClassifier
{
    // TYPE patterns (CardPaymentModel card) + property patterns + a "when" guard.
    public static string Describe(object? payment)
    {
        return payment switch
        {
            null => throw new ArgumentNullException(nameof(payment)),
            CardPaymentModel { Amount: <= 0 } or CashPaymentModel { Amount: <= 0 } => throw new ArgumentException("Amount must be more than 0.", nameof(payment)),
            CardPaymentModel card when card.Amount > 1000 => $"Large card payment {card.Amount:0.00} (card ...{card.LastFourDigits}) - needs approval",
            CardPaymentModel card => $"Card payment {card.Amount:0.00} (card ...{card.LastFourDigits})",
            CashPaymentModel { Amount: > 10000 } => throw new InvalidOperationException("Cash over 10,000 must be reported; not accepted here."),
            CashPaymentModel cash => $"Cash payment {cash.Amount:0.00}",
            _ => throw new NotSupportedException($"Unknown payment type: {payment.GetType().Name}"),
        };
    }

    // "is" with a pattern: the same patterns, but as a yes/no check instead of a switch.
    public static bool NeedsApproval(object? payment)
    {
        return payment is CardPaymentModel { Amount: > 1000 } or CashPaymentModel { Amount: > 5000 };
    }

    // "not" flips a pattern. A card's last four digits must be exactly 4 characters and not "0000".
    public static bool HasUsableCardDigits(CardPaymentModel? card)
    {
        return card is { LastFourDigits: { Length: 4 } and not "0000" };
    }
}
