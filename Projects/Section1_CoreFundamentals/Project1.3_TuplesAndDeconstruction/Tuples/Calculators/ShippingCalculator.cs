namespace Tuples.Calculators;

// SWITCHING ON A TUPLE: two inputs checked together, one rule per line. "_" = "anything".
// The compiler checks the rules top to bottom; the first match wins.
public static class ShippingCalculator
{
    public static decimal Cost(string country, bool express)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        return (country.ToUpperInvariant(), express) switch
        {
            ("US", false) => 5m,
            ("US", true) => 15m,
            ("CA" or "MX", false) => 12m,
            (_, true) => 40m,  // any other country, express
            (_, false) => 25m, // any other country, standard
        };
    }
}
