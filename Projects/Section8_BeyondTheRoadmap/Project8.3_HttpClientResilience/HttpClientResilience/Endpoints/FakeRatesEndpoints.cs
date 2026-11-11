using HttpClientResilience.Models;

namespace HttpClientResilience.Endpoints;

// A pretend EXTERNAL rates service, hosted in the same app so the demo runs offline.
// It fails on purpose about half the time (503), so you can watch the retries work.
public static class FakeRatesEndpoints
{
    private static readonly Dictionary<string, decimal> ratesFromUsd = new()
    {
        ["USD"] = 1.00m,
        ["EUR"] = 0.92m,
        ["GBP"] = 0.79m,
        ["JPY"] = 151.20m,
    };

    public static void MapFakeRatesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/fake-rates-service/rates/{from}/{to}", (string from, string to, ILogger<Program> logger) =>
        {
            if (Random.Shared.NextDouble() < 0.5)
            {
                logger.LogWarning("Fake rates service: failing on purpose (503)");
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            if (!ratesFromUsd.TryGetValue(from, out decimal fromRate) || !ratesFromUsd.TryGetValue(to, out decimal toRate))
            {
                return Results.NotFound();
            }
            return Results.Ok(new ExchangeRateModel(from, to, Math.Round(toRate / fromRate, 6)));
        })
        .ExcludeFromDescription();
    }
}
