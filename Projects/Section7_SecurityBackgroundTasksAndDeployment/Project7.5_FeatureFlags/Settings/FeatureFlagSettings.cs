namespace FeatureFlags.Settings;

// The "FeatureFlags" section of appsettings.json, e.g. { "NewCheckout": false }.
public class FeatureFlagSettings : Dictionary<string, bool>
{
}
