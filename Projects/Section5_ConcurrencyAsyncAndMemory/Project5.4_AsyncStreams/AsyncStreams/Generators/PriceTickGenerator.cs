using System.Runtime.CompilerServices;
using AsyncStreams.Models;

namespace AsyncStreams.Generators;

// An ASYNC ITERATOR: "async" + returns IAsyncEnumerable<T> + "yield return". It can AWAIT between items
// (a delay here; a network call or database page in real code). The caller loops with "await foreach".
public static class PriceTickGenerator
{
    // [EnumeratorCancellation]: lets the caller's ".WithCancellation(token)" reach this "cancellationToken".
    // Without it, a token given via WithCancellation is silently ignored (see Variations/CancellationVariation).
    public static async IAsyncEnumerable<PriceTickModel> StreamAsync(
        string symbol,
        int count,
        TimeSpan delay,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        decimal price = 100m;
        for (int number = 1; number <= count; number++)
        {
            await Task.Delay(delay, cancellationToken); // throws TaskCanceledException when cancelled
            price += number % 2 == 0 ? -1.5m : 2m;
            yield return new PriceTickModel(number, symbol, price);
        }
    }
}
