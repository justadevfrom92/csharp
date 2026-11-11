# Project 8.4 — Rate Limiting

| Folder | What |
|---|---|
| `RateLimiting/` | API with two rate-limit policies and one endpoint that is never limited |
| `RateLimiting.Tests/` | Tests for each policy |

```bash
dotnet run --project RateLimiting
```

```bash
for i in 1 2 3 4 5 6 7; do curl -s -o /dev/null -w "%{http_code} " -H "X-Client-Id: alice" localhost:5000/api/quotes; done
```

Prints `200 200 200 200 200 429 429` — the 6th request in 10 seconds is rejected, with `Retry-After: 10`.

```bash
dotnet test RateLimiting.Tests
```

## Why rate limiting

Without it, one client (a buggy script, a scraper, an attacker) can send thousands of requests a second and
slow the app down for everyone, or run up your bill. A rate limiter answers **429 Too Many Requests** before the
endpoint does any work.

## What's set up (`Program.cs`)

| Policy | Limiter | Rule | On |
|---|---|---|---|
| `per-client-fixed-window` | **Fixed window**, **partitioned** by `X-Client-Id` | 5 requests per 10 s **per client** | `/api/quotes` |
| `one-report-at-a-time` | **Concurrency** | 1 request running at a time | `/api/report` |
| — | `DisableRateLimiting()` | never limited | `/health` |

- **Partitioned:** each client id gets its **own** counter, so one noisy client can't use up everybody's quota.
  (In real apps the partition key is usually the logged-in user, an API key, or the IP address.)
- **`QueueLimit = 0`:** over the limit → rejected straight away instead of being made to wait.
- **`OnRejected`** adds `Retry-After: <seconds>` so well-behaved clients know when to come back.
- **`UseRateLimiter()`** must come before the endpoints; `RequireRateLimiting("name")` picks the policy per endpoint.
- **Health checks are never limited** — if they got 429, the load balancer would think the app is down.

## The four built-in limiters

| Limiter | How it counts | Good for |
|---|---|---|
| **Fixed window** (used) | N requests per window; the count resets at the end of each window | Simple quotas ("100 per minute") |
| Sliding window | Window split into segments that roll forward | Smoother: no burst of 2N right at a window boundary |
| Token bucket | Tokens refill at a steady rate; each request spends one | Allowing short bursts but a steady average |
| **Concurrency** (used) | How many requests are running **right now** | Protecting expensive work (reports, exports) |

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Fixed window per client | 5 requests → all 200 | 6th → 429 with `Retry-After` ≤ 10 s, while **bob** still gets 200 and `/health` still works | no `X-Client-Id` → 400 every time, **never** 429 (uses no quota) |
| Concurrency | two reports one after another → both 200 | second report while the first is running → 429 | `seconds=abc` → 400, and the slot is **released** (next report → 200) |

Every test starts a new app, so every limiter starts with a full quota.
