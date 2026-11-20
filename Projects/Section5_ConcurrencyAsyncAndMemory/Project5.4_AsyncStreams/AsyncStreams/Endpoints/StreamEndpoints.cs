using AsyncStreams.Generators;
using AsyncStreams.Models;
using AsyncStreams.Services;

namespace AsyncStreams.Endpoints;

public static class StreamEndpoints
{
    public static void MapStreamEndpoints(this IEndpointRouteBuilder app)
    {
        // Returning IAsyncEnumerable<T>: ASP.NET Core writes the JSON array PIECE BY PIECE as items arrive,
        // so the client sees the first tick after ~1 delay, not after all of them. The request's own token
        // (client disconnected) is passed in, so the loop stops when nobody is listening.
        app.MapGet("/ticks", (string? symbol, int? count, int? delayMs, CancellationToken cancellationToken) =>
            PriceTickGenerator.StreamAsync(symbol ?? "ACME", Math.Clamp(count ?? 5, 0, 100), TimeSpan.FromMilliseconds(Math.Clamp(delayMs ?? 500, 0, 5000)), cancellationToken));

        // Database rows streamed straight to the client.
        app.MapGet("/orders/big", (decimal? min, OrderStreamService service) =>
            service.ReadBigOrders(min ?? 100m));

        app.MapGet("/orders/big/sum", async (decimal? min, OrderStreamService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.SumBigOrdersAsync(min ?? 100m, cancellationToken)));

        app.MapGet("/orders/customers", async (int? count, OrderStreamService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.FirstCustomersAsync(Math.Clamp(count ?? 2, 0, 100), cancellationToken)));
    }
}
