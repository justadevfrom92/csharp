namespace Banking.Domain.Variations;

// Money as a one-line POSITIONAL record, with the check in a property initializer.
// It looks just as safe as MoneyValueObject, but a "with" expression copies the object and sets properties
// through their init setters WITHOUT running the check:  valid with { Amount = -5 }  ->  -5, no error.
// MoneyValueObject avoids this by having get-only properties (no init), so "with" can't change them.
public record MoneyPositionalVariation(decimal Amount, string Currency)
{
    public decimal Amount { get; init; } = Amount >= 0 ? Amount : throw new ArgumentException("Money cannot be negative.", nameof(Amount));
}
