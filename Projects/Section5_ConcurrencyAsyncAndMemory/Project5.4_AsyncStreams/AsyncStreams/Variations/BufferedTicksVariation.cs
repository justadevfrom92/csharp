using AsyncStreams.Generators;
using AsyncStreams.Models;

namespace AsyncStreams.Variations;

// The same ticks as /ticks, but COLLECTED FIRST with ToListAsync(): the client gets nothing until the
// last tick is made, and the whole list sits in server memory. Mapped at /variations/ticks/buffered.
public static class BufferedTicksVariation
{
    public static void MapBufferedTicks(this IEndpointRouteBuilder app)
    {
        app.MapGet("/variations/ticks/buffered", async (int? count, int? delayMs, CancellationToken cancellationToken) =>
        {
            List<PriceTickModel> ticks = await PriceTickGenerator
                .StreamAsync("ACME", Math.Clamp(count ?? 5, 0, 100), TimeSpan.FromMilliseconds(Math.Clamp(delayMs ?? 500, 0, 5000)), cancellationToken)
                .ToListAsync(cancellationToken);
            return TypedResults.Ok(ticks);
        });

        // A stream that FAILS halfway. The 200 status and the first items are already sent when the error
        // happens, so the client gets 200 and then the body is cut off mid-JSON - not a 500. Check for errors BEFORE streaming.
        app.MapGet("/variations/ticks/broken", () => FailAfterTwo());
    }

    public static async IAsyncEnumerable<PriceTickModel> FailAfterTwo()
    {
        yield return new PriceTickModel(1, "ACME", 100m);
        await Task.Delay(10);
        yield return new PriceTickModel(2, "ACME", 101m);
        throw new InvalidOperationException("Price feed went down.");
    }
}
