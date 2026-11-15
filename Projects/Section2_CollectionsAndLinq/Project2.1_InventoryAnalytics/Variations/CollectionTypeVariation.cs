using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

// Same idea, different collection type - and what each one lets the CALLER do.
public static class CollectionTypeVariation
{
    // Queue vs List used as a queue: same order out. List.RemoveAt(0) shifts every item (slow for big lists).
    public static List<int> DrainWithQueue(int[] orders)
    {
        var queue = new Queue<int>(orders);
        var result = new List<int>();
        while (queue.Count > 0)
        {
            result.Add(queue.Dequeue());
        }
        return result;
    }

    public static List<int> DrainWithListRemoveAt(int[] orders)
    {
        var list = new List<int>(orders);
        var result = new List<int>();
        while (list.Count > 0)
        {
            result.Add(list[0]);
            list.RemoveAt(0);
        }
        return result;
    }

    // Unique values: HashSet (keeps first-seen order for small sets, but ORDER IS NOT GUARANTEED),
    // Distinct (keeps first-seen order), List + Contains (O(n) per add).
    public static List<string> UniqueWithHashSet(IEnumerable<string> values)
    {
        return new HashSet<string>(values, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static List<string> UniqueWithDistinct(IEnumerable<string> values)
    {
        return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    // "Read-only" is not one thing:
    //   AsReadOnly()      -> a read-only VIEW: the caller can't change it, but changes to the original SHOW THROUGH.
    //   (IReadOnlyList)   -> just a cast: the caller could cast it BACK to List and change it.
    //   ToImmutableList() -> a real COPY that can never change.
    //   ToFrozenSet()     -> a copy that can never change, built for very fast lookups (read-heavy data).
    public static ReadOnlyCollection<string> AsReadOnlyView(List<string> source)
    {
        return source.AsReadOnly();
    }

    public static IReadOnlyList<string> AsReadOnlyInterface(List<string> source)
    {
        return source;
    }

    public static ImmutableList<string> AsImmutableCopy(List<string> source)
    {
        return source.ToImmutableList();
    }

    public static FrozenSet<string> AsFrozenSet(List<string> source)
    {
        return source.ToFrozenSet();
    }
}
