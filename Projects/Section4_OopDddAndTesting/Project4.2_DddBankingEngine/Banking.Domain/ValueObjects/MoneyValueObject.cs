namespace Banking.Domain.ValueObjects;

// VALUE OBJECT: no id, it is equal to another MoneyValueObject if amount and currency match.
// "record" gives us that equality for free, and get-only properties make it immutable.
public record MoneyValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyValueObject(decimal amount, string currency)
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

    public static MoneyValueObject Zero(string currency)
    {
        return new MoneyValueObject(0, currency);
    }

    // Returns a NEW MoneyValueObject. The original is never changed.
    public MoneyValueObject Add(MoneyValueObject other)
    {
        CheckSameCurrency(other);
        return new MoneyValueObject(Amount + other.Amount, Currency);
    }

    public MoneyValueObject Subtract(MoneyValueObject other)
    {
        CheckSameCurrency(other);
        return new MoneyValueObject(Amount - other.Amount, Currency); // throws if the result would be negative
    }

    public bool IsLessThan(MoneyValueObject other)
    {
        CheckSameCurrency(other);
        return Amount < other.Amount;
    }

    public override string ToString()
    {
        return $"{Amount:0.00} {Currency}";
    }

    private void CheckSameCurrency(MoneyValueObject other)
    {
        if (other.Currency != Currency)
        {
            throw new InvalidOperationException($"Cannot mix {Currency} and {other.Currency}.");
        }
    }
}
