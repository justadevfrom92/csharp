namespace OperatorsAndLambdas.ValueObjects;

// OPERATOR OVERLOADING: lets Money be used with + - * < > like a built-in number:
//   MoneyValueObject total = price * 3 + shipping;
// instead of price.Multiply(3).Add(shipping).
// Rule of thumb: only overload an operator when its meaning is OBVIOUS (adding two amounts of money).
// "+" on two Customers would be a puzzle, so don't.
public record MoneyValueObject : IComparable<MoneyValueObject>
{
    public MoneyValueObject(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter code like USD.", nameof(currency));
        }
        Amount = decimal.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }
    public string Currency { get; }

    // Binary operators are always "public static", and at least one parameter must be this type.
    public static MoneyValueObject operator +(MoneyValueObject left, MoneyValueObject right)
    {
        CheckSameCurrency(left, right);
        return new MoneyValueObject(left.Amount + right.Amount, left.Currency);
    }

    public static MoneyValueObject operator -(MoneyValueObject left, MoneyValueObject right)
    {
        CheckSameCurrency(left, right);
        return new MoneyValueObject(left.Amount - right.Amount, left.Currency);
    }

    // Unary minus: -refund
    public static MoneyValueObject operator -(MoneyValueObject money)
    {
        ArgumentNullException.ThrowIfNull(money);
        return new MoneyValueObject(-money.Amount, money.Currency);
    }

    // Money * number (and number * money, so both orders work).
    public static MoneyValueObject operator *(MoneyValueObject money, decimal factor)
    {
        ArgumentNullException.ThrowIfNull(money);
        return new MoneyValueObject(money.Amount * factor, money.Currency);
    }

    public static MoneyValueObject operator *(decimal factor, MoneyValueObject money)
    {
        return money * factor;
    }

    // Comparison operators come in PAIRS: if you write <, the compiler makes you write > too.
    public static bool operator <(MoneyValueObject left, MoneyValueObject right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(MoneyValueObject left, MoneyValueObject right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(MoneyValueObject left, MoneyValueObject right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(MoneyValueObject left, MoneyValueObject right)
    {
        return left.CompareTo(right) >= 0;
    }

    // == and != are written for us by "record" (same Amount and Currency = equal).

    // EXPLICIT conversion: (decimal)money. Explicit because it LOSES information (the currency),
    // so the caller has to ask for it on purpose. An implicit one would happen silently.
    public static explicit operator decimal(MoneyValueObject money)
    {
        ArgumentNullException.ThrowIfNull(money);
        return money.Amount;
    }

    public int CompareTo(MoneyValueObject? other)
    {
        ArgumentNullException.ThrowIfNull(other);
        CheckSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public override string ToString()
    {
        return $"{Amount:0.00} {Currency}";
    }

    private static void CheckSameCurrency(MoneyValueObject left, MoneyValueObject right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException($"Cannot mix {left.Currency} and {right.Currency}.");
        }
    }
}
