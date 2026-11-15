using FeatureFlags.Attributes;
using FeatureFlags.Constants;
using FeatureFlags.Services;
using Microsoft.AspNetCore.Mvc;

namespace FeatureFlags.Variations;

// The same "ProductReviews is off" answered different ways, plus the two rollout styles. Try /variations/flags/....
[Route("variations/flags")]
public class FlagGateVariation : Controller
{
    private readonly FeatureFlagService flags;

    public FlagGateVariation(FeatureFlagService flags)
    {
        this.flags = flags;
    }

    // ORIGINAL: the action filter attribute -> 404, so the feature looks like it doesn't exist yet.
    [HttpGet("attribute-404")]
    [RequireFeatureFlag(FeatureNameConstants.ProductReviews)]
    public IActionResult WithAttribute()
    {
        return Content("reviews");
    }

    // Checked inside the action, answering 403: tells the user the page EXISTS but they can't have it - leaks
    // that a feature is coming. Sometimes that's what you want ("join the beta").
    [HttpGet("manual-403")]
    public IActionResult WithManualForbidden()
    {
        if (!flags.IsEnabled(FeatureNameConstants.ProductReviews))
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Reviews aren't available yet.");
        }
        return Content("reviews");
    }

    // Redirect to the old page instead of an error: the user never sees a dead end.
    [HttpGet("manual-redirect")]
    public IActionResult WithRedirect()
    {
        if (!flags.IsEnabled(FeatureNameConstants.ProductReviews))
        {
            return RedirectToAction("Index", "Shop");
        }
        return Content("reviews");
    }

    // 5 page loads by the same user: stable hash vs random.
    [HttpGet("rollout")]
    public IActionResult Rollout(string user = "ann", int percent = 50)
    {
        IEnumerable<bool> stable = Enumerable.Range(0, 5).Select(_ => PercentageRolloutVariation.IsOnForUser(user, percent));
        IEnumerable<bool> random = Enumerable.Range(0, 5).Select(_ => PercentageRolloutVariation.IsOnRandomly(percent));
        return Json(new { user, percent, stableByUserId = stable, randomPerRequest = random });
    }
}
