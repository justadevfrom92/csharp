using HttpClientResilience.Clients;
using HttpClientResilience.Endpoints;
using HttpClientResilience.Settings;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

ExchangeRateSettings rateSettings = builder.Configuration.GetSection("ExchangeRates").Get<ExchangeRateSettings>()!;

// IHttpClientFactory + a TYPED client. The factory creates and reuses the underlying connections correctly
// (a `new HttpClient()` per request would run out of sockets; one static HttpClient forever misses DNS changes).
builder.Services.AddHttpClient<ExchangeRateClient>(client =>
    {
        client.BaseAddress = new Uri(rateSettings.BaseAddress);
    })
    // RESILIENCE PIPELINE: wraps every request this client sends. Strategies run outside-in:
    // retry -> timeout per attempt -> the actual HTTP call.
    .AddResilienceHandler("exchange-rates", pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            // By default retries: 5xx, 408 Request Timeout, 429 Too Many Requests, network errors and timeouts.
            // It does NOT retry 4xx like 400/404: asking again won't change the answer.
            MaxRetryAttempts = rateSettings.MaxRetryAttempts,
            BackoffType = DelayBackoffType.Exponential, // wait 200 ms, 400 ms, 800 ms...
            UseJitter = true,                           // ...plus a little randomness, so clients don't retry in sync
            Delay = TimeSpan.FromMilliseconds(rateSettings.FirstRetryDelayMilliseconds),
        });
        pipeline.AddTimeout(TimeSpan.FromMilliseconds(rateSettings.AttemptTimeoutMilliseconds)); // per attempt
    });

builder.Services.AddProblemDetails();

var app = builder.Build();
// A BadHttpRequestException (e.g. amount=ten) already knows it's a 400 - keep that instead of a 500.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest ? badRequest.StatusCode : 500,
});
app.MapConvertEndpoints();
app.MapFakeRatesEndpoints();
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
