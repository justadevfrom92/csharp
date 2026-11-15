namespace SimpleLoanAndDateCalculator.Variations;

// FIVE ways to write "use the name, or 'Customer' if there isn't one".
// The first four treat ONLY null as missing. The last one also treats "" and "   " as missing.
public static class NullHandlingVariation
{
    // ORIGINAL style (Program.cs uses ??=, which is the same idea): shortest.
    public static string WithNullCoalescing(string? name)
    {
        return name ?? "Customer";
    }

    // "is null": same result. Can't be fooled by a class that overloads == (see WithEqualsNull).
    public static string WithIsNull(string? name)
    {
        if (name is null)
        {
            return "Customer";
        }
        return name;
    }

    // "== null": same result for string. For your OWN class with an overloaded == operator (9.1),
    // the overload runs and could answer differently - that's why "is null" is preferred.
    public static string WithEqualsNull(string? name)
    {
        return name == null ? "Customer" : name;
    }

    // Ternary with "is not null": same result, reads positive-first.
    public static string WithIsNotNullTernary(string? name)
    {
        return name is not null ? name : "Customer";
    }

    // DIFFERENT BEHAVIOUR: also replaces "" and "   ". Use it for user input, where an empty answer means "nothing".
    public static string WithIsNullOrWhiteSpace(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? "Customer" : name;
    }
}
