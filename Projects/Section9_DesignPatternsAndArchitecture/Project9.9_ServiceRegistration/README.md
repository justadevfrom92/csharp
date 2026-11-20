# Project 9.9 — Service Registration (`builder.Services`) & `RegisterAllExistingJobs`

Every common way to register services with ASP.NET Core's dependency injection, plus a custom
**`builder.Services.RegisterAllExistingJobs()`** that finds every background job class in the project and runs them.

```bash
cd ServiceRegistration
dotnet run
```

Then open `http://localhost:5099/jobs`, `/lifetimes`, `/greeting/Ann`, `/repositories`, `/notifiers`, `/prices/3`, `/health`.

```bash
cd ServiceRegistration.Tests
dotnet test
```

## `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterAllExistingJobs();   // every IJob class + the runner that runs them
builder.Services.AddShopServices();           // our own registrations, grouped in one extension method

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

## `RegisterAllExistingJobs` — how it works

`RegisterAllExistingJobs` is **not** a built-in .NET method. It's our own **extension method** on
`IServiceCollection` (`Extensions/JobServiceCollectionExtensions.cs`) that does **assembly scanning**:

1. Look through the compiled project for every class that implements `IJob` and **isn't abstract**
   (`BaseJob` is skipped).
2. Register each as `IJob` with `TryAddEnumerable`, so calling it twice doesn't register (and run) jobs twice.
3. Register what jobs need: `JobRunStore`, `JobSchedulerService`, `TimeProvider`, the `JobRunner` settings
   (checked at startup) and `JobRunnerBackgroundService`.

**Why:** a new job is just **a new class** in `Jobs/`. Nobody has to remember to add a line to `Program.cs`.
**Cost:** it's "magic": you can't see the list of jobs in `Program.cs`. `GET /jobs` shows what was found.
(Libraries like Scrutor do assembly scanning in general; Quartz.NET and Hangfire are full job schedulers.)

**Jobs use scoped services safely.** `JobSchedulerService` is a singleton, but a job may need a **scoped** service
(here a repository; in real apps a `DbContext`). A singleton must never keep a scoped service, so for every run it
creates a **new scope** with `IServiceScopeFactory`, gets the jobs inside it, and disposes the scope afterwards.

## Every `builder.Services` registration style

| # | Style | Code | Where |
|---|---|---|---|
| 1 | **Lifetimes** | `AddSingleton` (one for the app), `AddScoped` (one per request), `AddTransient` (new every time) | `AddShopServices`, `GET /lifetimes` proves it |
| 2 | **Keyed** | `AddKeyedSingleton<T>("key")`, get with `[FromKeyedServices("key")]` or `GetRequiredKeyedService<T>("key")` | `AddShopServices` (also 7.3) |
| 3 | **Options** | `AddOptions<T>().BindConfiguration("Shop").ValidateDataAnnotations().ValidateOnStart()`, `PostConfigure<T>` | `AddShopServices`, `RegisterAllExistingJobs` |
| 4 | **Factory** | `AddSingleton(serviceProvider => new GreetingService(...))` — for constructors DI can't fill (a plain `string`) | `AddShopServices` |
| 5 | **Open generic** | `AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>))` — one line for every `T` | `AddShopServices` |
| 6 | **Several implementations** | register `INotifier` twice → inject `IEnumerable<INotifier>` for all, `INotifier` for the **last** one | `AddShopServices` |
| 7 | **Decorator** | register the real class as itself, then the interface as a factory that wraps it | `AddShopServices` (price cache) |
| 8 | **TryAdd** | `TryAddSingleton` / `TryAddEnumerable`: only if not already registered — lets the app or a test register its own first | both extension methods |
| 9 | **Replace / RemoveAll** | `services.Replace(ServiceDescriptor.Singleton<IPriceService, Fake>())`, `services.RemoveAll<INotifier>()` | the tests (`ConfigureTestServices`) |
| 10 | **Instance** | `AddSingleton<INotifier>(alreadyCreatedObject)` | the tests |
| 11 | **Hosted service** | `AddHostedService<JobRunnerBackgroundService>()` | `RegisterAllExistingJobs` (also 7.3) |
| 12 | **Assembly scanning** | `RegisterAllExistingJobs()` | `Extensions/JobServiceCollectionExtensions.cs` |
| 13 | **Grouping in extension methods** | `builder.Services.AddShopServices()` returns `IServiceCollection` so calls chain | `Extensions/` |
| 14 | **Framework services** | `AddProblemDetails`, `AddHealthChecks`, `AddHttpClient`, `AddMemoryCache` | `Program.cs` |
| 15 | **Self-checks** | `ValidateScopes` (no scoped service from the root), `ValidateOnBuild` (every service can be created, checked at startup) | `Program.cs` |

Other `builder.Services` calls used elsewhere in this repo: `AddControllersWithViews` (3.x), `AddRazorPages` (8.6),
`AddRazorComponents` (8.7), `AddDbContext`/`AddDbContextPool` (6.1, 8.2), `AddAuthentication`/`AddAuthorization` (7.1, 8.2),
`AddSession` (6.4), `AddOutputCache` (7.4), `AddStackExchangeRedisCache` (7.9), `AddOpenApi`/`AddValidation` (8.1),
`AddRateLimiter` (8.4), `AddSignalR` (8.8), `AddGrpc` (8.9), `AddIdentityCore` (8.2), `AddHttpClient<T>().AddResilienceHandler` (8.3).

## Code snippets

**`RegisterAllExistingJobs`**, from `Extensions/JobServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection RegisterAllExistingJobs(this IServiceCollection services, params Assembly[] assemblies)
{
    ArgumentNullException.ThrowIfNull(services);
    Assembly[] toScan = assemblies.Length > 0 ? assemblies : [typeof(IJob).Assembly];

    IEnumerable<Type> jobTypes = toScan
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => type.IsClass && !type.IsAbstract && typeof(IJob).IsAssignableFrom(type));

    foreach (Type jobType in jobTypes)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IJob), jobType));
    }

    services.TryAddSingleton(TimeProvider.System);
    services.TryAddSingleton<JobRunStore>();
    services.TryAddSingleton<JobSchedulerService>();
    services.AddHostedService<JobRunnerBackgroundService>();
    return services;
}
```

**A job: just a class**, from `Jobs/RefreshPricesJob.cs`

```csharp
public class RefreshPricesJob : BaseJob
{
    public RefreshPricesJob(IPriceService prices)
    {
        this.prices = prices;
    }

    public override TimeSpan Interval => TimeSpan.FromSeconds(10);

    public override Task RunAsync(CancellationToken cancellationToken)
    {
        _ = prices.GetPrice(1);
        return Task.CompletedTask;
    }
}
```

**A singleton using scoped services: a new scope per run**, from `Services/JobSchedulerService.cs`

```csharp
using IServiceScope scope = scopeFactory.CreateScope();
IEnumerable<IJob> jobs = scope.ServiceProvider.GetServices<IJob>();
foreach (IJob job in jobs)
{
    await job.RunAsync(cancellationToken);
}
```

**The registration styles**, from `Extensions/ShopServiceCollectionExtensions.cs`

```csharp
services.AddKeyedSingleton<LifetimeProbeService>("singleton");
services.AddKeyedScoped<LifetimeProbeService>("scoped");
services.AddKeyedTransient<LifetimeProbeService>("transient");

services.AddOptions<ShopSettings>().BindConfiguration("Shop").ValidateDataAnnotations().ValidateOnStart();
services.PostConfigure<ShopSettings>(settings => settings.ShopName = settings.ShopName.Trim());

services.AddSingleton(serviceProvider =>
    new GreetingService(serviceProvider.GetRequiredService<IOptions<ShopSettings>>().Value.ShopName));

services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));

services.TryAddEnumerable(ServiceDescriptor.Singleton<INotifier, EmailNotifier>());
services.TryAddEnumerable(ServiceDescriptor.Singleton<INotifier, SmsNotifier>());

services.AddSingleton<SlowPriceService>();
services.AddSingleton<IPriceService>(serviceProvider => new CachingPriceServiceDecorator(
    serviceProvider.GetRequiredService<SlowPriceService>(),
    serviceProvider.GetRequiredService<IMemoryCache>()));

services.TryAddSingleton(TimeProvider.System);
```

**Replacing registrations in a test**, from `ServiceRegistration.Tests/RegistrationsTest.cs`

```csharp
HttpClient client = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
{
    services.Replace(ServiceDescriptor.Singleton<IPriceService, FixedPriceService>());
    services.RemoveAll<INotifier>();
    services.AddSingleton<INotifier>(onlyEmail);
})).CreateClient();
```

## Variations

Other ways to register jobs, the look-alike "get a service" methods, and the naming conventions — in
`ServiceRegistration/Variations/`, checked by `ServiceRegistration.Tests/VariationsTest.cs`.

**Registering the jobs** (`JobRegistrationVariation`)

| Way | Finds | When you add a new job | The difference |
|---|---|---|---|
| `RegisterAllExistingJobs()` — every non-abstract class implementing `IJob` (original) | all 3 | nothing to do | "magic": the list isn't visible in code |
| `RegisterJobsByHand()` — one `TryAddEnumerable` line per job | the 2 written down; **`SendDailyReportJob` was "forgotten" and never runs** | add a line | explicit, easy to forget |
| `.AddJob<TJob>()` generic helper | what you list (same 3 as scanning in the test) | add a line | shorter; `where TJob : IJob` is checked by the compiler |
| by **name** — classes ending in `Job` (`FindJobsByName`) | **wrong both ways**: found `ReportJob` (not a job), missed `NightlyCleanup` (a job) | name it right | conventions are guesses |
| by **attribute** `[AutoRegisterVariation]` (`RegisterJobsWithAttribute`) | only marked classes | add the attribute | opt-in: nothing registered by accident |

**Getting a service out** (`ResolveVariation`) — normal code takes services in its **constructor** instead; these are for
factories, background loops and tests:

| Method | Service not registered | The difference |
|---|---|---|
| `GetService<T>()` | `null` | "optional" |
| `GetRequiredService<T>()` | throws `InvalidOperationException` | "must exist" — the usual choice |
| `GetServices<T>()` | empty list (2 notifiers here) | every registration of `T` |
| `GetKeyedService<T>(key)` / `GetRequiredKeyedService<T>(key)` | `null` / throws | by key |
| `ActivatorUtilities.CreateInstance<T>(provider)` | **creates it anyway** | for a class that isn't registered, filling its constructor from the container |

**A lifetime mistake the container catches** — a singleton that takes a scoped service ("captive dependency"):

| Provider | Result |
|---|---|
| built with no validation | **works, silently wrong**: the scoped object lives forever |
| built with `ValidateScopes = true, ValidateOnBuild = true` (original `Program.cs`) | `AggregateException` at startup |

Also found by this test: options with `BindConfiguration("Shop")` need an `IConfiguration` in the container
(`WebApplication` adds one; a bare `ServiceCollection` doesn't), and `ValidateDataAnnotations` then rejected an empty `SupportEmail`.

**What the method names mean** (the conventions across ASP.NET Core):

| Prefix | Means | Examples |
|---|---|---|
| `Add...` on `builder.Services` | **register** services (before `Build()`) | `AddControllers`, `AddDbContext`, `AddHttpClient`, `RegisterAllExistingJobs` (our own, same idea) |
| `TryAdd...` | register only if nothing is registered yet | `TryAddSingleton`, `TryAddEnumerable` |
| `Configure<T>` / `PostConfigure<T>` | set **options** | `Configure<FeatureFlagSettings>(...)` |
| `Use...` on `app` | add **middleware** to the request pipeline (order matters) | `UseRouting`, `UseAuthentication`, `UseRateLimiter` |
| `Map...` on `app` | add **endpoints** | `MapGet`, `MapControllers`, `MapHub`, `MapGrpcService`, `MapHealthChecks` |
| `With...` / `Require...` on an endpoint | add **metadata/rules** to one endpoint | `WithTags`, `RequireAuthorization`, `RequireRateLimiting` |
| `Build()` / `Run()` | finish registering / start the app | `builder.Build()`, `app.Run()` |

**Reflection** (`ReflectionVariation`, on the small class `StaffDiscountVariation`) — reading types at run time:

| Call | Gives | Watch out |
|---|---|---|
| `type.GetProperties()` | `Code`, `Price` | — |
| `type.GetMethods(Public \| Instance \| DeclaredOnly)` + skip `IsSpecialName` | `PriceForCustomer` | without the flags you also get `ToString`, `GetHashCode`... and the hidden `get_Price`/`set_Price` |
| `PropertyInfo.GetValue(target)` | the value, as `object` | a misspelt name (`"Prise"`) is only found when it runs |
| `PropertyInfo.SetValue(target, value)` | writes it | **ignores `init`**: wrote `Code` although `discount.Code = ...` doesn't compile; wrong type (`"cheap"` into `decimal`) → `ArgumentException` |
| `GetMethod(name, NonPublic \| Instance).Invoke(...)` | calls a **private** method | an exception inside arrives wrapped in `TargetInvocationException`; wrong argument count → `TargetParameterCountException` |
| `Assembly.LoadFrom(path).GetTypes()` | **plugins**: the 3 jobs in a `.dll` found by file path | missing file → `FileNotFoundException`; not a .NET dll → `BadImageFormatException`; the file stays loaded until the app stops |

How slow it is: 5.3 `ReflectionCostVariation`.

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `IJob` interface + `abstract class BaseJob` | Scanning looks for the interface; the base class gives a default `Name`; abstract classes are skipped. | — |
| Jobs registered **transient** | A fresh job per run, inside that run's scope, so it can use scoped services. | Singleton jobs couldn't use scoped services. |
| `TryAddEnumerable` for jobs and notifiers | Adds an implementation only once, even if called twice. | `AddTransient` twice = the job runs twice. |
| `JobSchedulerService` separate from `JobRunnerBackgroundService` | Tests call `RunDueJobsAsync` directly, with a fake clock. | Testing a `BackgroundService` means waiting for real time. |
| `TimeProvider` (and `FakeTimeProvider` in tests) | "Is the job due?" depends on time; a fake clock makes tests exact. | `DateTimeOffset.UtcNow` can't be controlled. |
| `params Assembly[] assemblies` | Scan this project by default, or any assemblies you pass (tests pass one with no jobs). | — |
| Extension methods returning `IServiceCollection` | Same style as the built-in `Add...()` calls; chains. | A long `Program.cs`. |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `RegisterAllExistingJobs` | finds the 3 concrete jobs (transient), skips `BaseJob`, registers the runner | called twice → no duplicates; an assembly without jobs → none | `null` services |
| Scheduler | all run first; 10 s later only the 10-second job | nothing due → 0 runs; a failing job is recorded, the others still run | cancellation stops without recording a failure |
| Registrations (whole app) | lifetimes (within and across requests), factory, open generic, all/last notifier, decorator | `Replace`, `RemoveAll`, instance registration from a test; `/prices/0` → 400 | invalid settings → app refuses to start (`ValidateOnStart`); a service with a missing dependency → caught at startup (`ValidateOnBuild`) |
