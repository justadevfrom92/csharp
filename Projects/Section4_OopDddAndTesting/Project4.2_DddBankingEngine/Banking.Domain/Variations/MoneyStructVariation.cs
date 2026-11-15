namespace Banking.Domain.Variations;

// Money as a STRUCT. Equality by value works, and it lives on the stack (no allocation).
// The catch: every struct has a hidden "all zeros" value - default(MoneyStructVariation) - that SKIPS the
// constructor. Its Currency is null even though the constructor forbids that. (See WHY_THESE_TYPES.md.)
public readonly record struct MoneyStructVariation
{
    public MoneyStructVariation(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Money cannot be negative.", nameof(amount));
        }
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter code like USD.", nameof(currency));
        }
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }
    public string Currency { get; }
}
