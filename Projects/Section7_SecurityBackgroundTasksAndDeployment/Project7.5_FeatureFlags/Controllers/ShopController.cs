using FeatureFlags.Attributes;
using FeatureFlags.Constants;
using FeatureFlags.Services;
using Microsoft.AspNetCore.Mvc;

namespace FeatureFlags.Controllers;

public class ShopController : Controller
{
    private readonly FeatureFlagService featureFlags;

    public ShopController(FeatureFlagService featureFlags)
    {
        this.featureFlags = featureFlags;
    }

    public IActionResult Index()
    {
        return View();
    }

    // One URL, two behaviours: the flag decides which checkout runs.
    public IActionResult Checkout()
    {
        if (featureFlags.IsEnabled(FeatureNameConstants.NewCheckout))
        {
            return Content("NEW one-page checkout (behind the NewCheckout flag)");
        }
        return Content("Old three-step checkout");
    }

    // The whole page only exists while the flag is on.
    [RequireFeatureFlag(FeatureNameConstants.ProductReviews)]
    public IActionResult Reviews()
    {
        return Content("Product reviews page (behind the ProductReviews flag)");
    }

    [HttpGet("/flags")]
    public IActionResult Flags()
    {
        return Json(new
        {
            holidayBanner = featureFlags.IsEnabled(FeatureNameConstants.HolidayBanner),
            newCheckout = featureFlags.IsEnabled(FeatureNameConstants.NewCheckout),
            productReviews = featureFlags.IsEnabled(FeatureNameConstants.ProductReviews),
        });
    }
}
