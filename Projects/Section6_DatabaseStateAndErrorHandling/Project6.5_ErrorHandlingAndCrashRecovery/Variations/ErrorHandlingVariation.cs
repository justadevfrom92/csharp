using Microsoft.AspNetCore.Mvc;

namespace ErrorHandlingAndCrashRecovery.Variations;

// The same crash ("database password in the message") handled four ways. Run in Production
// (ASPNETCORE_ENVIRONMENT=Production) and call /variations/errors/....
[Route("variations/errors")]
public class ErrorHandlingVariation : Controller
{
    private const string SecretMessage = "Database login failed for user 'sa' with password 'Hunter2'";

    // Not caught here -> ApiErrorExceptionHandlerVariation (IExceptionHandler) answers with JSON ProblemDetails.
    [HttpGet("handler")]
    public IActionResult ThrowToHandler()
    {
        throw new InvalidOperationException(SecretMessage);
    }

    // Caught by the [HideErrorsFilterVariation] exception filter on this action.
    [HttpGet("filter")]
    [HideErrorsFilterVariation]
    public IActionResult ThrowToFilter()
    {
        throw new InvalidOperationException(SecretMessage);
    }

    // try/catch in the action: full control, but repeated in every action - and easy to leak the message by mistake.
    [HttpGet("try-catch")]
    public IActionResult WithTryCatch()
    {
        try
        {
            throw new InvalidOperationException(SecretMessage);
        }
        catch (InvalidOperationException)
        {
            return StatusCode(503, "Please try again later.");
        }
    }

    // The MISTAKE: returning exception.Message to the user. This is what sanitization exists to prevent.
    [HttpGet("leaky")]
    public IActionResult LeakingTheMessage()
    {
        try
        {
            throw new InvalidOperationException(SecretMessage);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(500, exception.Message);
        }
    }
}
