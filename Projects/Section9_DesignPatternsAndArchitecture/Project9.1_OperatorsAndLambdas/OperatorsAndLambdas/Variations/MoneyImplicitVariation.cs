namespace OperatorsAndLambdas.Variations;

// Money with an IMPLICIT conversion from decimal (the original MoneyValueObject has none, and only an EXPLICIT
// one TO decimal). Implicit means the compiler converts silently:  MoneyImplicitVariation price = 19.99m;
// Handy - but the currency is GUESSED (USD), so a euro amount written as a plain number becomes dollars.
public record MoneyImplicitVariation(decimal Amount, string Currency)
{
    public static implicit operator MoneyImplicitVariation(decimal amount)
    {
        return new MoneyImplicitVariation(amount, "USD");
    }

    // A named method instead of an operator (the 4.2 style: Add / Subtract). Same result as "+";
    // longer to read in formulas, but easy to find and impossible to misread.
    public MoneyImplicitVariation Add(MoneyImplicitVariation other)
    {
        if (other.Currency != Currency)
        {
            throw new InvalidOperationException($"Cannot mix {Currency} and {other.Currency}.");
        }
        return new MoneyImplicitVariation(Amount + other.Amount, Currency);
    }
}
