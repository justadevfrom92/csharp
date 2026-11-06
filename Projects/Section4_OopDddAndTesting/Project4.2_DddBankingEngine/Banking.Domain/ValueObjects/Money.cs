namespace Banking.Domain.ValueObjects;

// VALUE OBJECT: no id, it is equal to another Money if amount and currency match.
// "record" gives us that equality for free, and get-only properties make it immutable.
public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Money cannot be negative.", nameof(amount));
        }
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter code like USD.", nameof(currency));
        }
        Amount = decimal.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency)
    {
        return new Money(0, currency);
    }

    // Returns a NEW Money. The original is never changed.
    public Money Add(Money other)
    {
        CheckSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        CheckSameCurrency(other);
        return new Money(Amount - other.Amount, Currency); // throws if the result would be negative
    }

    public bool IsLessThan(Money other)
    {
        CheckSameCurrency(other);
        return Amount < other.Amount;
    }

    public override string ToString()
    {
        return $"{Amount:0.00} {Currency}";
    }

    private void CheckSameCurrency(Money other)
    {
        if (other.Currency != Currency)
        {
            throw new InvalidOperationException($"Cannot mix {Currency} and {other.Currency}.");
        }
    }
}
