using System.Collections;
using Iterators.Enumerators;

namespace Iterators.Collections;

// Our own collection that works with foreach, LINQ, and "new ShelfCollection { "a", "b" }".
// IEnumerable<T> needs just ONE method: GetEnumerator(). Add() + IEnumerable enables the { } initializer.
public class ShelfCollection : IEnumerable<string>
{
    private readonly List<string> items = new();

    public void Add(string item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item);
        items.Add(item);
    }

    // Hands out a COPY (ToArray), so adding items during a loop doesn't break it - unlike List<T>, which throws.
    public IEnumerator<string> GetEnumerator()
    {
        return new ShelfEnumerator(items.ToArray());
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
