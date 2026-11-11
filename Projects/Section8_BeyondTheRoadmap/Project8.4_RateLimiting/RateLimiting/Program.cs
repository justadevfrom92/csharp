using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using RateLimiting.Constants;
using RateLimiting.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    // What a rejected request gets: 429 Too Many Requests + "Retry-After: <seconds>" when the limiter knows it.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }
        return ValueTask.CompletedTask;
    };

    // FIXED WINDOW, PARTITIONED per client: every X-Client-Id gets its own "5 per 10 seconds".
    // One noisy client can't use up everybody else's quota.
    options.AddPolicy(RateLimitPolicyConstants.PerClientFixedWindow, context =>
    {
        string? clientId = context.Request.Headers[RateLimitPolicyConstants.ClientIdHeader];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            // No id: don't count it here; the endpoint answers 400 anyway.
            return RateLimitPartition.GetNoLimiter("no-client-id");
        }
        return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = RateLimitPolicyConstants.RequestsPerWindow,
            Window = TimeSpan.FromSeconds(RateLimitPolicyConstants.WindowSeconds),
            QueueLimit = 0, // over the limit -> rejected straight away, not made to wait
        });
    });

    // CONCURRENCY: at most 1 report running at the same moment, for everyone together.
    options.AddConcurrencyLimiter(RateLimitPolicyConstants.OneReportAtATime, limiter =>
    {
        limiter.PermitLimit = 1;
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest ? badRequest.StatusCode : 500,
});
app.UseRateLimiter(); // must come before the endpoints it protects
app.MapApiEndpoints();
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
