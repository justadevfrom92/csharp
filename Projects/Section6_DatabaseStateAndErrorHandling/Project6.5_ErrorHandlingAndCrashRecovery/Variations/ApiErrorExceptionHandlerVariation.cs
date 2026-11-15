using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ErrorHandlingAndCrashRecovery.Variations;

// IExceptionHandler (.NET 8+): a CLASS that the exception handler middleware asks first. Returning true means
// "handled"; false means "not mine - carry on" (to the /Error/ServerError page, the original).
// Here: errors under /variations/ get a JSON ProblemDetails answer instead of an HTML page. Registered in Program.cs.
public class ApiErrorExceptionHandlerVariation : IExceptionHandler
{
    private readonly ILogger<ApiErrorExceptionHandlerVariation> logger;

    public ApiErrorExceptionHandlerVariation(ILogger<ApiErrorExceptionHandlerVariation> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // GOTCHA (found while writing this): with UseExceptionHandler("/Error/ServerError"), Request.Path has ALREADY been
        // changed to "/Error/ServerError" when this runs. The URL that actually failed is in IExceptionHandlerPathFeature.
        string failedPath = httpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? httpContext.Request.Path;
        if (!failedPath.StartsWith("/variations", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        logger.LogError(exception, "Variation endpoint failed");
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 500,
            Title = "Something went wrong.",
            Extensions = { ["errorId"] = httpContext.TraceIdentifier }, // no exception message: it may hold secrets
        }, cancellationToken);
        return true;
    }
}
