namespace Tuples.Variations;

// "Give back a total AND a count", five ways. PriceStatsCalculator.Summarize (original) uses a named tuple.
public static class MultipleResultsVariation
{
    // OUT parameters: the old way (int.TryParse still uses it). Can't be used in async methods or lambdas easily.
    public static void WithOut(IReadOnlyList<decimal> prices, out decimal total, out int count)
    {
        total = prices.Sum();
        count = prices.Count;
    }

    // Tuple<T1, T2> (the CLASS, .NET 4): read as Item1/Item2 - no names; a heap object; can't be changed.
    public static Tuple<decimal, int> WithTupleClass(IReadOnlyList<decimal> prices)
    {
        return Tuple.Create(prices.Sum(), prices.Count);
    }

    // UNNAMED value tuple: works, but the caller only sees Item1/Item2.
    public static (decimal, int) WithUnnamedTuple(IReadOnlyList<decimal> prices)
    {
        return (prices.Sum(), prices.Count);
    }

    // A RECORD: a real named type. Best when the result travels (APIs, JSON, many callers).
    public static PriceTotalsVariation WithRecord(IReadOnlyList<decimal> prices)
    {
        return new PriceTotalsVariation(prices.Sum(), prices.Count);
    }

    // An ANONYMOUS type "new { Total, Count }" has no name you can write as a return type, so it only works
    // INSIDE one method (LINQ Select, as in 2.1). Returned as object, the caller can't read it without reflection or dynamic.
    public static object WithAnonymousType(IReadOnlyList<decimal> prices)
    {
        return new { Total = prices.Sum(), Count = prices.Count };
    }
}
