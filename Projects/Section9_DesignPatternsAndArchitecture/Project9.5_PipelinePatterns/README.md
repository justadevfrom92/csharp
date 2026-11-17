# Project 9.5 — Pipeline Patterns: Pipes & Filters, Chain of Responsibility, Decorator Pipeline, Interceptor / AOP

Four ways to run a request through **several steps**. ASP.NET Core itself is built from these, so seeing them
small makes the framework easier to understand.

```bash
cd PipelinePatterns
dotnet run
```

```bash
cd PipelinePatterns.Tests
dotnet test
```

## The four, side by side

| Pattern | Who runs? | Here | Folder | The same idea in ASP.NET Core / this repo |
|---|---|---|---|---|
| **Pipes and filters** | **every** step, in order, each changing the data | clean a log line: trim → mask email → mask card → cut length | `Filters/`, `Pipelines/` | LINQ chains (2.1), the channel pipeline (5.2), Dataflow (9.6) |
| **Chain of responsibility** | **one** step handles it; the rest pass it on | expense approval: team lead → manager → director | `Handlers/` | middleware that ends the request early (`ShortCircuit`, 7.4; rate limiter 8.4), exception filters |
| **Decorator pipeline** | every layer, **wrapping** the next one (before AND after) | payment: logging → validation → retry → bank | `Decorators/`, `Processors/` | the email sender chain (7.3), `HttpClient` handlers + resilience (8.3) |
| **Interceptor / AOP** | one generic class sees **every method call** of any interface | log + time every call to `IStockService` | `Interceptors/` | EF Core interceptors (6.1), MVC action filters (7.5), endpoint filters (8.1) |

### Pipes and filters

Each filter is `string in → string out` and knows nothing about the others. Add, remove or **re-order** them
without touching the rest; test each one alone. Order can matter: masking **before** cutting the length makes
sure a card number is never cut in half and left unmasked.

### Chain of responsibility

```csharp
teamLead.SetNext(manager).SetNext(director);
teamLead.Approve(new ExpenseModel("Ann", 4_000m, "Laptop"));   // -> "Approved by manager"
```

The first handler that **can** deal with the request does, and the chain stops. Nobody can → rejected.
The sender only knows the first handler. Adding "CFO up to 1,000,000" = one class and one `SetNext`.

### Decorator pipeline

```
Logging ─► Validation ─► Retry ─► FlakyPaymentProcessor (the bank)
```

Every layer implements `IPaymentProcessor` and wraps the next. **The order is a design decision**:
validation is outside retry, so an invalid payment is rejected **once** and never retried or sent to the bank;
logging is outermost, so it records the final outcome of every call, including validation failures.
(Polly / `AddResilienceHandler` in 8.3 is exactly a decorator pipeline around `HttpClient`.)

### Interceptor / AOP

**AOP (Aspect-Oriented Programming):** "cross-cutting concerns" — logging, timing, security checks, caching —
are needed in **every** method. Writing them in each method buries the real code; a decorator per interface
is a lot of repeated classes. An **interceptor** is one class that wraps **any** interface:

```csharp
IStockService stock = LoggingInterceptor<IStockService>.Wrap(new StockService(), log);
stock.Reserve("KB-01", 3);    // log: "Reserve(KB-01, 3) ok in 0 ms"
```

`DispatchProxy` (built into .NET) generates a class at runtime that implements the interface and sends every
call to `Invoke`. Two details handled here:
- Reflection wraps exceptions in `TargetInvocationException`; the interceptor **unwraps** it (with
  `ExceptionDispatchInfo`, keeping the original stack trace) so callers still catch `InvalidOperationException`.
- Only calls that go **through the proxy** are seen. `Reserve` calling its own `GetStock` inside the class isn't
  logged. (Same with EF interceptors and MVC filters: they only see what passes through them.)

Libraries like Castle DynamicProxy do the same with more features (classes, not just interfaces).

## Code snippets

**Pipes and filters**, from `Pipelines/TextPipeline.cs` and `Program.cs`

```csharp
public string Run(string text)
{
    string current = text;
    foreach (ITextFilter filter in filters)
    {
        current = filter.Apply(current); // one filter's output is the next one's input
    }
    return current;
}

TextPipeline cleanLogLine = new TextPipeline()
    .Add(new TrimWhitespaceFilter())
    .Add(new MaskEmailFilter())
    .Add(new MaskCardNumberFilter())
    .Add(new MaximumLengthFilter(60));
```

**Chain of responsibility**, from `Handlers/BaseApprovalHandler.cs`

```csharp
public BaseApprovalHandler SetNext(BaseApprovalHandler nextHandler)
{
    next = nextHandler;
    return nextHandler;
}

public string Approve(ExpenseModel expense)
{
    if (CanApprove(expense))
    {
        return $"Approved by {Role}";
    }
    if (next == null)
    {
        return $"Rejected: {expense.Amount:0.00} is more than anyone can approve";
    }
    return next.Approve(expense);
}
```

**Decorator pipeline**, from `Decorators/` and `Program.cs`

```csharp
public string Process(PaymentModel payment)        // RetryPaymentProcessorDecorator
{
    for (int attempt = 1; ; attempt++)
    {
        try
        {
            return inner.Process(payment);
        }
        catch (TimeoutException) when (attempt < maximumAttempts)
        {
        }
    }
}

IPaymentProcessor payments =
    new LoggingPaymentProcessorDecorator(
        new ValidationPaymentProcessorDecorator(
            new RetryPaymentProcessorDecorator(new FlakyPaymentProcessor(2), maximumAttempts: 3)),
        log);
```

**Interceptor with `DispatchProxy`**, from `Interceptors/LoggingInterceptor.cs`

```csharp
public class LoggingInterceptor<T> : DispatchProxy where T : class
{
    public static T Wrap(T target, List<string> log)
    {
        T proxy = Create<T, LoggingInterceptor<T>>();
        var interceptor = (LoggingInterceptor<T>)(object)proxy;
        interceptor.target = target;
        interceptor.log = log;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            object? result = targetMethod!.Invoke(target, args);
            log.Add($"{targetMethod.Name} ok in {timer.ElapsedMilliseconds} ms");
            return result;
        }
        catch (TargetInvocationException wrapped) when (wrapped.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(wrapped.InnerException).Throw();
            throw;
        }
    }
}
```

## Variations

A lighter version of each pipeline, in `PipelinePatterns/Variations/`, compared with the original by
`PipelinePatterns.Tests/VariationsTest.cs`.

| Pattern | Original | Variation | Same result? | The difference |
|---|---|---|---|---|
| Pipes and filters | `TextPipeline` + `ITextFilter` classes | `filters.Aggregate(text, (current, filter) => filter(current))` over `Func<string, string>` (`TextPipelineVariation`) | ✅ | LINQ `Aggregate` **is** a pipeline; filter objects still work via `filter.Apply` |
| Chain of responsibility | linked handler objects + `SetNext` | table of `(Limit, Role)` + loop (`ApprovalTableVariation`) | ✅ for all 5 amounts, edges included | enough when each handler only compares a number; objects win when a handler calls services or partly handles and passes on |
| Decorator pipeline | one class per layer wrapping `IPaymentProcessor` | **functions**: each step gets `next` and returns a wrapped step (`PaymentMiddlewareVariation`) | ✅ same result **and same log** | exactly how ASP.NET Core builds its middleware pipeline (`app.Use((context, next) => ...)`) |
| Interceptor / AOP | `DispatchProxy` for any interface | a hand-written decorator per interface | — | no reflection, faster, but one class per interface |

**Order matters** (same two function steps, swapped, payment of -5):

| Order | Result | Log |
|---|---|---|
| logging → validation | rejected | `start A-2` (logging saw it first) |
| validation → logging | rejected | empty (rejected before logging) |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `ITextFilter` interface | Many interchangeable filters. | A `List<Func<string, string>>` would also work (see 9.1); classes are easier to name and test alone. |
| `abstract class BaseApprovalHandler` | The "handle or pass on" logic is shared; each handler only says its role and limit. | Repeating the pass-on code in every handler. |
| `const decimal Limit` | Fixed rule; tests can refer to it. | — |
| Decorators take the inner `IPaymentProcessor` in the constructor | Layers are stacked by construction, any order. | Inheritance (`RetryingBankProcessor : BankProcessor`) can't be re-ordered or combined freely. |
| `DispatchProxy` | Built into .NET, no package. Works for any interface. | A hand-written decorator per interface. |
| `ExceptionDispatchInfo.Capture(...).Throw()` | Re-throws the inner exception with its original stack trace. | `throw inner;` resets the stack trace. |
| `List<string> log` | Simple, easy to assert in tests. | `ILogger` in a real app. |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Pipes and filters | every filter runs in order | clean text unchanged; long text cut; no filters = no change | `null` text / filter; max length < 4 |
| Chain of responsibility | the right role approves (limits inclusive) | over every limit, or chain with no next → rejected | amount 0; `null` expense / next handler |
| Decorator pipeline | 2 failures + 3 attempts → paid, logged | invalid amount never reaches the bank; 5 failures → gives up after 3 | `null` payment; 0 attempts |
| Interceptor | calls logged and still work; internal calls not seen | business error → original exception type, logged | unknown SKU; `null` target / log |
