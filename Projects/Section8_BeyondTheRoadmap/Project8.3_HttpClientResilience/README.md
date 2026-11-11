# Project 8.3 — IHttpClientFactory + Resilience (retries, timeouts)

| Folder | What |
|---|---|
| `HttpClientResilience/` | Currency converter that calls an exchange-rate service through a typed `HttpClient` |
| `HttpClientResilience.Tests/` | Tests with a scripted fake network: exact failure patterns, every call counted |

```bash
dotnet run --project HttpClientResilience --urls http://localhost:5083
```

```bash
curl "localhost:5083/convert?from=usd&to=eur&amount=100"
```

The app also hosts a pretend rates service (`/fake-rates-service/...`) that **fails half the time on purpose**.
Run the `curl` a few times: every answer is a 200, and the log shows the failures the retries hid.

```bash
dotnet test HttpClientResilience.Tests
```

## Why `IHttpClientFactory`

| Approach | Problem |
|---|---|
| `new HttpClient()` for every request | Each one opens new connections; under load you **run out of sockets** |
| One `static HttpClient` forever | Keeps connections forever, so it **misses DNS changes** (e.g. after a failover) |
| `IHttpClientFactory` (`AddHttpClient<T>`) | Reuses connections **and** refreshes them regularly. Configuration (base address, resilience) lives in one place |

**Typed client** (`Clients/ExchangeRateClient.cs`): a normal class that gets a ready-configured `HttpClient` in its
constructor and only knows "how to ask for a rate". (The other styles: *named* clients —
`factory.CreateClient("rates")` — and plain `IHttpClientFactory.CreateClient()`. Typed clients are the easiest
to read and to test.)

## The resilience pipeline (`Program.cs`)

```csharp
.AddResilienceHandler("exchange-rates", pipeline =>
{
    pipeline.AddRetry(new HttpRetryStrategyOptions { MaxRetryAttempts = 3, BackoffType = DelayBackoffType.Exponential, UseJitter = true, Delay = ... });
    pipeline.AddTimeout(TimeSpan.FromMilliseconds(...)); // per attempt
});
```

- **Retry:** on 5xx, 408, 429, network errors and timeouts. **Not** on 400/404 — asking again won't change
  "your request is wrong" or "that doesn't exist".
- **Exponential backoff:** wait 200 ms, 400 ms, 800 ms — a struggling service gets more breathing room each time.
- **Jitter:** a little randomness, so 1,000 clients don't all retry at the same millisecond and knock the service over again.
- **Timeout per attempt:** a hanging call is cut off and retried, instead of making the user wait forever.
- **After the last retry:** the endpoint catches `HttpRequestException` (kept failing) **and**
  `TimeoutRejectedException` (kept timing out) and returns a clear **503**, never a crash.

All numbers come from `appsettings.json` (`ExchangeRates` section), so tests can make the delays 1 ms.
For a ready-made bundle (retry + timeouts + **circuit breaker** + rate limiter), use
`.AddStandardResilienceHandler()` instead.

## Bugs the tests found

1. **"Every attempt too slow" returned 500.** When the timeout strategy gives up it throws Polly's
   `TimeoutRejectedException`, not `HttpRequestException`. The `catch` now handles both → 503.
2. **`amount=ten` returned 500** — the same `BadHttpRequestException` issue as Project 8.1. Fixed the same way
   (`StatusCodeSelector` on `UseExceptionHandler`) → 400.

## Tests (every behaviour has all three kinds)

The tests swap the typed client's network handler for `Fakes/ScriptedRatesHandlerFake`, which returns scripted
answers in order and **counts every call** — so "1 try + 3 retries" is checked exactly, with no real network.

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Input | valid → 92.00 EUR, **1** call | amount 0 → 400, **0** calls | `US`, `123`, missing `from`, `amount=ten` → 400, **0** calls |
| Retry | 503, 500, then OK → 200 after **3** calls | always 503 → gives up after **4** calls (1 + 3 retries) → 503; unknown pair 404 → **not** retried → 404 | service answers 400 → **not** retried (1 call) → 503 |
| Timeout per attempt | 50 ms answer → used | one 5 s answer → cut off at 300 ms and retried → 200 in < 3 s | always 5 s → 4 attempts, all cut off → 503 |
