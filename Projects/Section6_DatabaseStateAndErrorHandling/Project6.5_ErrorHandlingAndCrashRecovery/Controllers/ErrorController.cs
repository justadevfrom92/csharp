using System.Diagnostics;
using ErrorHandlingAndCrashRecovery.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ErrorHandlingAndCrashRecovery.Controllers;

// Excluded from API docs/filters; only reached through the error middleware.
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        this.logger = logger;
    }

    [Route("/Error/ServerError")]
    public IActionResult ServerError()
    {
        IExceptionHandlerPathFeature? errorDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        string errorId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        // The FULL details go to the log (only developers/ops can read it)...
        logger.LogError(errorDetails?.Error, "Unhandled error {ErrorId} on {Path}", errorId, errorDetails?.Path);

        // ...the user only gets an id to quote to support. No message, no stack trace.
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View(new ServerErrorViewModel { ErrorId = errorId });
    }

    [Route("/Error/Status/{statusCode:int}")]
    public IActionResult Status(int statusCode)
    {
        var reExecuteDetails = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        logger.LogWarning("Status {StatusCode} for {Path}", statusCode, reExecuteDetails?.OriginalPath);

        Response.StatusCode = statusCode;
        if (statusCode == 404)
        {
            return View("NotFound");
        }
        return View("OtherStatus", new StatusCodeErrorViewModel { StatusCode = statusCode });
    }
}
