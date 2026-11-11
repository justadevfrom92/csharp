using System.Diagnostics;

namespace OutputCachingCatalog.Middlewares;

// IMiddleware-based ("factory-based") middleware. Differences from the convention-based kind:
//   - it implements the IMiddleware interface, so the compiler checks the method signature
//   - it must be REGISTERED in DI (builder.Services.AddTransient<RequestTimingMiddleware>()),
//     and a new one is created PER REQUEST - so it can safely take scoped services in its constructor
//   - "next" is passed to InvokeAsync instead of the constructor
// What it does: measures how long each request takes, adds X-Response-Time-Ms, and logs it.
public class RequestTimingMiddleware : IMiddleware
{
    private readonly ILogger<RequestTimingMiddleware> logger;

    public RequestTimingMiddleware(ILogger<RequestTimingMiddleware> logger)
    {
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var timer = Stopwatch.StartNew();

        // Headers must be set BEFORE the body starts being sent. OnStarting runs at exactly that moment,
        // after the controller has done its work, so the time is (almost) the full request time.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Response-Time-Ms"] = timer.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        await next(context);

        logger.LogInformation("{Method} {Path} -> {StatusCode} in {Milliseconds} ms",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, timer.ElapsedMilliseconds);
    }
}
