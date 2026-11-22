namespace Tuples.Calculators;

// RETURNING SEVERAL VALUES with a NAMED TUPLE: "(decimal Total, int Count, decimal Average)".
// No extra class needed; the caller reads result.Total or deconstructs: var (total, count, average) = ...
public static class PriceStatsCalculator
{
    public static (decimal Total, int Count, decimal Average) Summarize(IReadOnlyList<decimal> prices)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (prices.Count == 0)
        {
            return (0m, 0, 0m);
        }
        decimal total = prices.Sum();
        return (total, prices.Count, Math.Round(total / prices.Count, 2));
    }

    // Nullable tuple "(...)?": "no answer" for an empty list, instead of made-up zeros.
    public static (decimal Cheapest, decimal Dearest)? Range(IReadOnlyList<decimal> prices)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (prices.Count == 0)
        {
            return null;
        }
        return (prices.Min(), prices.Max());
    }
}
