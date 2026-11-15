using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ErrorHandlingAndCrashRecovery.Variations;

// An MVC EXCEPTION FILTER: catches exceptions thrown inside the actions it's applied to (one controller or action),
// before the middleware sees them. Narrower than the global handler; can't catch errors outside MVC.
public class HideErrorsFilterVariation : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        context.Result = new ObjectResult(new { error = "Handled by the exception filter.", type = context.Exception.GetType().Name })
        {
            StatusCode = StatusCodes.Status500InternalServerError,
        };
        context.ExceptionHandled = true; // stop it going further
    }
}
