// This whole app is ONE file. The program runs from the top.
// The classes it uses (InMemoryAccountRepository, ConsoleEventDispatcher, SingletonExample, ScopedExample,
// TransientExample, AppInfo, DemoSettings, AuditEventDispatcher, ReportBuilder) are defined at the BOTTOM of this file.
// (In C#, top-level code must come before any class definitions in the file.)

using Banking.Domain.Aggregates;
using Banking.Domain.Dispatchers;
using Banking.Domain.Events;
using Banking.Domain.Exceptions;
using Banking.Domain.Repositories;
using Banking.Domain.Services;
using Banking.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ========== PART 1: the banking demo ==========

// ---------- Dependency Injection setup ----------
var services = new ServiceCollection();
services.AddSingleton<IBankAccountRepository, InMemoryAccountRepository>(); // one shared store for the whole app
services.AddSingleton<IDomainEventDispatcher, ConsoleEventDispatcher>();
services.AddTransient<BankAccountService>();                               // new one each time, it holds no data
ServiceProvider provider = services.BuildServiceProvider();

BankAccountService bank = provider.GetRequiredService<BankAccountService>();

Guid accountId = bank.OpenAccount("Alice", "USD");
bank.Deposit(accountId, new MoneyValueObject(100, "USD"));
bank.Withdraw(accountId, new MoneyValueObject(30, "USD"));

try
{
    bank.Withdraw(accountId, new MoneyValueObject(500, "USD"));
}
catch (InsufficientFundsException exception) when (exception.Requested.Amount > 100) // exception filter
{
    Console.WriteLine($"Big withdrawal refused: {exception.Message}");
}
finally
{
    Console.WriteLine($"Final balance: {bank.GetBalance(accountId)}");
}

// ========== PART 2: every way to register a service ==========
Console.WriteLine();
Console.WriteLine("=== DI registrations ===");
var demoServices = new ServiceCollection();

// 1. The three LIFETIMES
demoServices.AddSingleton<SingletonExample>();   // ONE object for the whole app
demoServices.AddScoped<ScopedExample>();         // ONE object per scope (in ASP.NET Core: per web request)
demoServices.AddTransient<TransientExample>();   // a NEW object every time someone asks

// 2. FACTORY registration: you write the code that creates it (useful when the constructor needs values)
demoServices.AddSingleton<AppInfo>(serviceProvider => new AppInfo("Banking Demo", DateTime.UtcNow));

// 3. INSTANCE registration: you create the object yourself, DI just hands it out (always a singleton)
demoServices.AddSingleton(new DemoSettings { DefaultCurrency = "USD" });

// 4. SEVERAL implementations of one interface: ask for IEnumerable<T> to get them all
demoServices.AddSingleton<IDomainEventDispatcher, ConsoleEventDispatcher>();
demoServices.AddSingleton<IDomainEventDispatcher, AuditEventDispatcher>();

// 5. TryAdd...: only registers if NOTHING is registered for that type yet (libraries use this so
//    they don't overwrite what the app already set up). This one is ignored: AppInfo already exists.
demoServices.TryAddSingleton<AppInfo>(serviceProvider => new AppInfo("IGNORED", DateTime.MinValue));

// 6. Constructor injection: ReportBuilder asks for AppInfo + DemoSettings in its constructor, DI fills them in.
demoServices.AddTransient<ReportBuilder>();

// (Keyed registrations - AddKeyedSingleton("key") - are shown in Project 7.3.)

using ServiceProvider demoProvider = demoServices.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,   // catch "scoped service used outside a scope" mistakes
    ValidateOnBuild = true,  // check every registration can be built, right now, instead of later
});

Console.WriteLine("Lifetimes - same Id means the SAME object was handed out:");
for (int scopeNumber = 1; scopeNumber <= 2; scopeNumber++)
{
    using IServiceScope scope = demoProvider.CreateScope();
    IServiceProvider scoped = scope.ServiceProvider;
    Console.WriteLine($"  scope {scopeNumber}:");
    Console.WriteLine($"    Singleton  {scoped.GetRequiredService<SingletonExample>().ShortId} {scoped.GetRequiredService<SingletonExample>().ShortId}   (same everywhere)");
    Console.WriteLine($"    Scoped     {scoped.GetRequiredService<ScopedExample>().ShortId} {scoped.GetRequiredService<ScopedExample>().ShortId}   (same inside this scope only)");
    Console.WriteLine($"    Transient  {scoped.GetRequiredService<TransientExample>().ShortId} {scoped.GetRequiredService<TransientExample>().ShortId}   (new every time)");
}

AppInfo appInfo = demoProvider.GetRequiredService<AppInfo>();
Console.WriteLine($"Factory:  AppInfo.Name = {appInfo.Name} (the TryAdd one was ignored)");
Console.WriteLine($"Instance: DemoSettings.DefaultCurrency = {demoProvider.GetRequiredService<DemoSettings>().DefaultCurrency}");
Console.WriteLine($"Several:  {demoProvider.GetServices<IDomainEventDispatcher>().Count()} dispatchers registered; GetRequiredService gives the LAST one: {demoProvider.GetRequiredService<IDomainEventDispatcher>().GetType().Name}");
Console.WriteLine($"Constructor injection: {demoProvider.GetRequiredService<ReportBuilder>().Build()}");

// 7. The classic mistake ("captive dependency"): a SINGLETON that needs a SCOPED service.
//    The singleton lives forever, so it would keep ONE scoped object forever - e.g. one DbContext shared
//    by every web request. ValidateOnBuild catches it when the provider is built.
var brokenServices = new ServiceCollection();
brokenServices.AddScoped<ScopedExample>();
brokenServices.AddSingleton<SingletonThatNeedsScoped>();
try
{
    brokenServices.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
}
catch (AggregateException exception)
{
    Console.WriteLine("Captive dependency caught at startup:");
    Exception problem = exception.InnerExceptions[0];
    Console.WriteLine("  " + (problem.InnerException ?? problem).Message);
}

// ---------- PART 1 classes: simple implementations living OUTSIDE the domain ----------
class InMemoryAccountRepository : IBankAccountRepository
{
    private readonly Dictionary<Guid, BankAccountAggregate> accounts = new();

    public BankAccountAggregate? FindById(Guid id)
    {
        return accounts.GetValueOrDefault(id);
    }

    public void Save(BankAccountAggregate account)
    {
        accounts[account.Id] = account;
    }
}

class ConsoleEventDispatcher : IDomainEventDispatcher
{
    public void Dispatch(IDomainEvent domainEvent)
    {
        Console.WriteLine($"[event] {domainEvent}");
    }
}

// ---------- PART 2 classes: tiny classes used only to show the DI registrations ----------
class SingletonExample
{
    public string ShortId { get; } = Guid.NewGuid().ToString("N").Substring(0, 6);
}

class ScopedExample
{
    public string ShortId { get; } = Guid.NewGuid().ToString("N").Substring(0, 6);
}

class TransientExample
{
    public string ShortId { get; } = Guid.NewGuid().ToString("N").Substring(0, 6);
}

record AppInfo(string Name, DateTime StartedAtUtc);

class DemoSettings
{
    public string DefaultCurrency { get; set; } = "";
}

class AuditEventDispatcher : IDomainEventDispatcher
{
    public void Dispatch(IDomainEvent domainEvent)
    {
        Console.WriteLine($"[audit] {domainEvent.GetType().Name}");
    }
}

class ReportBuilder
{
    private readonly AppInfo appInfo;
    private readonly DemoSettings settings;

    public ReportBuilder(AppInfo appInfo, DemoSettings settings)
    {
        this.appInfo = appInfo;
        this.settings = settings;
    }

    public string Build()
    {
        return $"report from {appInfo.Name} in {settings.DefaultCurrency}";
    }
}

class SingletonThatNeedsScoped
{
    public SingletonThatNeedsScoped(ScopedExample scoped)
    {
    }
}
