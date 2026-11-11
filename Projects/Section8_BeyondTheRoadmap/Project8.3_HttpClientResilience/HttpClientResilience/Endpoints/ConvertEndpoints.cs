using System.Text.RegularExpressions;
using HttpClientResilience.Clients;
using HttpClientResilience.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Polly.Timeout;

namespace HttpClientResilience.Endpoints;

public static class ConvertEndpoints
{
    public static void MapConvertEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /convert?from=USD&to=EUR&amount=10
        app.MapGet("/convert", ConvertAsync);
    }

    private static async Task<Results<Ok<ConversionModel>, ValidationProblem, NotFound<string>, ProblemHttpResult>> ConvertAsync(
        string? from, string? to, decimal amount, ExchangeRateClient rateClient, CancellationToken cancellationToken)
    {
        // Check OUR input first, so bad requests never reach the other service.
        var errors = new Dictionary<string, string[]>();
        if (!IsCurrencyCode(from))
        {
            errors["from"] = ["Must be a 3-letter currency code like USD."];
        }
        if (!IsCurrencyCode(to))
        {
            errors["to"] = ["Must be a 3-letter currency code like EUR."];
        }
        if (amount <= 0)
        {
            errors["amount"] = ["Must be more than 0."];
        }
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        try
        {
            ExchangeRateModel? rate = await rateClient.GetRateAsync(from!.ToUpperInvariant(), to!.ToUpperInvariant(), cancellationToken);
            if (rate == null)
            {
                return TypedResults.NotFound($"No rate for {from}->{to}.");
            }
            return TypedResults.Ok(new ConversionModel(rate.From, rate.To, amount, rate.Rate, Math.Round(amount * rate.Rate, 2)));
        }
        catch (Exception exception) when (exception is HttpRequestException or TimeoutRejectedException)
        {
            // The rates service still failed after every retry: tell OUR caller clearly, don't crash.
            // HttpRequestException = it kept answering with errors; TimeoutRejectedException = every attempt was too slow.
            return TypedResults.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "The exchange-rate service is unavailable. Try again later.");
        }
    }

    private static bool IsCurrencyCode(string? code)
    {
        return code != null && Regex.IsMatch(code, "^[A-Za-z]{3}$");
    }
}
