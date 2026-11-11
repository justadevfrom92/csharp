namespace HttpClientResilience.Settings;

// Bound from the "ExchangeRates" section of appsettings.json.
public class ExchangeRateSettings
{
    public string BaseAddress { get; set; } = "";
    public int MaxRetryAttempts { get; set; } = 3;
    public int FirstRetryDelayMilliseconds { get; set; } = 200;
    public int AttemptTimeoutMilliseconds { get; set; } = 2000;
}
