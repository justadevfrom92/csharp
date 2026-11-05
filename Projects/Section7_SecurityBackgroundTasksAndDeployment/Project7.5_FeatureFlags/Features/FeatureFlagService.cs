using Microsoft.Extensions.Options;

namespace FeatureFlags.Features;

// Names in one place, so a typo is a compile error instead of a silently-off feature.
public static class FeatureNames
{
    public const string HolidayBanner = "HolidayBanner";
    public const string NewCheckout = "NewCheckout";
    public const string ProductReviews = "ProductReviews";
}

// The "FeatureFlags" section of appsettings.json, e.g. { "NewCheckout": false }.
public class FeatureFlagSettings : Dictionary<string, bool>
{
}

public class FeatureFlagService
{
    private readonly IOptionsMonitor<FeatureFlagSettings> flags;

    // IOptionsMonitor always returns the LATEST config, so editing appsettings.json
    // turns features on/off without restarting the app.
    public FeatureFlagService(IOptionsMonitor<FeatureFlagSettings> flags)
    {
        this.flags = flags;
    }

    public bool IsEnabled(string featureName)
    {
        // A missing flag counts as OFF. Unknown = safe.
        return flags.CurrentValue.TryGetValue(featureName, out bool isEnabled) && isEnabled;
    }
}
