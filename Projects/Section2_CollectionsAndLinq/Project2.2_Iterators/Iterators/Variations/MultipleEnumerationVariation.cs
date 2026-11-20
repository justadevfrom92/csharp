namespace Iterators.Variations;

// A LINQ query is a RECIPE, not a result. Every loop / Count() / First() runs it again from the start.
// "runs" counts how many times the source was read.
public static class MultipleEnumerationVariation
{
    public static IEnumerable<int> Source(int[] runs)
    {
        runs[0]++;
        for (int number = 1; number <= 5; number++)
        {
            yield return number;
        }
    }

    // Count() reads the source, then First() reads it AGAIN. With a database or a file that's two trips.
    public static (int Count, int First) TwiceOverQuery(int[] runs)
    {
        IEnumerable<int> even = Source(runs).Where(number => number % 2 == 0);
        return (even.Count(), even.First());
    }

    // ToList() runs the recipe ONCE and keeps the results.
    public static (int Count, int First) OnceWithToList(int[] runs)
    {
        List<int> even = Source(runs).Where(number => number % 2 == 0).ToList();
        return (even.Count, even[0]);
    }
}
