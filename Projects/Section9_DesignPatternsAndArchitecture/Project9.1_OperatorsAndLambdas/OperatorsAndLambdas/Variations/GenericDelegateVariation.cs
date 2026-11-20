namespace OperatorsAndLambdas.Variations;

// The NAMED generic delegate types .NET has besides Func/Action/Predicate, and a custom generic one.
// Each is just "a method with this shape" - the name says what it is FOR.
public static class GenericDelegateVariation
{
    // A custom GENERIC delegate: like NumberRule in DelegateSyntaxVariation, but for any input and output types.
    // Same shape as Func<T, TResult>; the name documents the job.
    public delegate TResult Transformer<T, TResult>(T input);

    // Comparison<T>: "which of two comes first?" -> negative (left first), 0 (same), positive (right first).
    // List.Sort accepts it directly.
    public static List<string> SortByLength(List<string> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        Comparison<string> byLength = (left, right) => left.Length.CompareTo(right.Length);
        var sorted = new List<string>(words);
        sorted.Sort(byLength);
        return sorted;
    }

    // The same comparison as a Func<string, string, int>. "sorted.Sort(byLength)" does NOT compile:
    // two delegate types with the same shape are still different types, so it must be wrapped.
    public static List<string> SortByLengthWithFunc(List<string> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        Func<string, string, int> byLength = (left, right) => left.Length.CompareTo(right.Length);
        var sorted = new List<string>(words);
        sorted.Sort(new Comparison<string>(byLength));
        return sorted;
    }

    // Converter<TInput, TOutput>: "turn one thing into another". List.ConvertAll accepts it.
    // LINQ's Select(Func<T, TResult>) does the same job and is what you'll usually see.
    public static List<int> Lengths(List<string> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        Converter<string, int> toLength = word => word.Length;
        return words.ConvertAll(toLength);
    }

    // Our own generic delegate used as a parameter.
    public static List<TResult> ApplyToAll<T, TResult>(List<T> items, Transformer<T, TResult> transform)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(transform);
        var results = new List<TResult>();
        foreach (T item in items)
        {
            results.Add(transform(item));
        }
        return results;
    }
}
