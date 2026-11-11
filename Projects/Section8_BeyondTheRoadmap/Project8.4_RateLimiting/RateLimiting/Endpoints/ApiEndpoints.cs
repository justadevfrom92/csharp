using RateLimiting.Constants;

namespace RateLimiting.Endpoints;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        // 5 requests per 10 seconds, counted separately for every X-Client-Id.
        app.MapGet("/api/quotes", (HttpContext context) =>
        {
            string? clientId = context.Request.Headers[RateLimitPolicyConstants.ClientIdHeader];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Results.Problem(statusCode: 400, title: $"Send the {RateLimitPolicyConstants.ClientIdHeader} header.");
            }
            return Results.Ok(new { clientId, quote = "Simplicity is prerequisite for reliability." });
        })
        .RequireRateLimiting(RateLimitPolicyConstants.PerClientFixedWindow);

        // An expensive report: only ONE may run at a time (it takes ?seconds=N, default 1).
        app.MapGet("/api/report", async (double? seconds, CancellationToken cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds ?? 1), cancellationToken);
            return Results.Ok("Report finished");
        })
        .RequireRateLimiting(RateLimitPolicyConstants.OneReportAtATime);

        // Health checks must never be blocked, or the load balancer would think the app is down.
        app.MapGet("/health", () => Results.Ok("Healthy"))
            .DisableRateLimiting();
    }
}
