using Microsoft.Extensions.Options;

namespace FeatureFlags.Features;

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
