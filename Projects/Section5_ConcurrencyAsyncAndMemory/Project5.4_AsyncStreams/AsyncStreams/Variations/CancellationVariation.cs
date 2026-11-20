using System.Runtime.CompilerServices;

namespace AsyncStreams.Variations;

// Three ways an async iterator can (or can't) be stopped with a CancellationToken.
public static class CancellationVariation
{
    // No token parameter at all: ".WithCancellation(token)" compiles but is IGNORED - the loop runs to the end
    // unless the caller breaks out itself.
    public static async IAsyncEnumerable<int> WithoutToken(int count)
    {
        for (int number = 1; number <= count; number++)
        {
            await Task.Delay(10);
            yield return number;
        }
    }

    // Token parameter WITH [EnumeratorCancellation] (the original PriceTickGenerator): works both when passed
    // as an argument and through .WithCancellation(token).
    public static async IAsyncEnumerable<int> WithAttribute(int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int number = 1; number <= count; number++)
        {
            await Task.Delay(10, cancellationToken);
            yield return number;
        }
    }

    // Checking the token yourself instead of passing it to Task.Delay: stops QUIETLY (no exception) at the
    // next check. Callers can't tell "finished" from "cancelled".
    public static async IAsyncEnumerable<int> StopQuietly(int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int number = 1; number <= count; number++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            await Task.Delay(10);
            yield return number;
        }
    }
}
