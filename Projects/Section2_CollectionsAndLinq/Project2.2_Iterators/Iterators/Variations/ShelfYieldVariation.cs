using System.Collections;

namespace Iterators.Variations;

// The same collection as ShelfCollection, but GetEnumerator uses "yield return" - no ShelfEnumerator class needed.
// Difference: it loops over the LIVE list, so adding during a loop throws (List<T>'s own check).
public class ShelfYieldVariation : IEnumerable<string>
{
    private readonly List<string> items = new();

    public void Add(string item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item);
        items.Add(item);
    }

    public IEnumerator<string> GetEnumerator()
    {
        foreach (string item in items)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
