using FeatureFlags.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace FeatureFlags.Attributes;

// Usage: [RequireFeatureFlag(FeatureNameConstants.NewCheckout)]  -> 404 when the feature is off.
public class RequireFeatureFlagAttribute : ActionFilterAttribute
{
    private readonly string featureName;

    public RequireFeatureFlagAttribute(string featureName)
    {
        this.featureName = featureName;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var featureFlags = context.HttpContext.RequestServices.GetRequiredService<FeatureFlagService>();
        if (!featureFlags.IsEnabled(featureName))
        {
            // 404, not 403: the feature should look like it doesn't exist yet.
            context.Result = new NotFoundResult();
        }
    }
}
