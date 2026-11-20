using System.Collections;

namespace Iterators.Enumerators;

// What "yield return" writes for you, BY HAND: a cursor that remembers where it is.
// foreach does: MoveNext() -> true? read Current -> repeat; then Dispose().
public class ShelfEnumerator : IEnumerator<string>
{
    private readonly string[] items;
    private int position = -1; // -1 = "before the first item"

    public ShelfEnumerator(string[] items)
    {
        this.items = items;
    }

    public string Current
    {
        get
        {
            if (position < 0 || position >= items.Length)
            {
                throw new InvalidOperationException("Call MoveNext() first, and only read Current while it returned true.");
            }
            return items[position];
        }
    }

    // The old non-generic interface needs this too (returns object).
    object IEnumerator.Current
    {
        get
        {
            return Current;
        }
    }

    public bool MoveNext()
    {
        if (position < items.Length)
        {
            position++;
        }
        return position < items.Length;
    }

    public void Reset()
    {
        position = -1;
    }

    public void Dispose()
    {
        // Nothing to release. A file- or database-backed enumerator would close it here.
    }
}
