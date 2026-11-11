namespace RateLimiting.Constants;

public static class RateLimitPolicyConstants
{
    public const string PerClientFixedWindow = "per-client-fixed-window";
    public const string OneReportAtATime = "one-report-at-a-time";
    public const string ClientIdHeader = "X-Client-Id";
    public const int RequestsPerWindow = 5;
    public const int WindowSeconds = 10;
}
