# Project 4.2 — DDD Banking Engine + xUnit Tests

| Folder | What |
|---|---|
| `Banking.Domain` | The business rules. No database, no web, no NuGet packages. |
| `Banking.Tests` | xUnit tests (`[Fact]`, `[Theory]`) for every rule. |
| `Banking.Demo` | Console app wiring it up with Dependency Injection. |

## Run

```bash
dotnet test Banking.Tests
```

```bash
dotnet test Banking.Tests --collect:"XPlat Code Coverage"
```

```bash
dotnet run --project Banking.Demo
```

## The three kinds of test (every behaviour has all three)

| Kind | Input | Expected |
|---|---|---|
| **PositivePositive** | Valid: right type, right format, within the rules | Succeeds |
| **PositiveNegative** | Well-formed, but breaks a business rule | Correctly refused (or a "no" answer), and **nothing changes** |
| **NegativeNegative** | Malformed or missing: `null`, zero, wrong format, unknown id | Rejected cleanly with a clear error |

Test names say which one they are: `Behaviour_Kind_Situation_ExpectedResult`.

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Create money | `10.456 usd` → `10.46 USD` | `-1 USD` → refused | currency `""`, `"US"`, `"DOLLARS"`, `null` → rejected |
| Equals | `10 USD` = `10.00 usd` | `10 USD` ≠ `11 USD` | compared with `null` → not equal |
| Add | `10 + 5 = 15`, original unchanged | USD + EUR → refused | `Add(null)` → rejected |
| Subtract | `10 - 4 = 6` | `10 - 15` → refused (would be negative) | `Subtract(null)` → rejected |
| IsLessThan | `5 < 10` → true | `10 < 5` → false | `IsLessThan(null)` → rejected |
| Open account | `"  Alice  "` → trimmed, zero balance, event | owner `"   "` → refused | owner `null`, currency `"US"`/`null` → rejected |
| Deposit | 100 → balance 100, event | into a closed account / wrong currency → refused, balance unchanged | `0`, `0.001`, `null` → rejected |
| Withdraw | 30 of 100 → 70, event | 500 of 100 → `InsufficientFundsException`, balance unchanged | `0`, `null` → rejected |
| Close | zero balance → closed, event | still has money → refused, stays open | already closed → rejected |
| Service: OpenAccount / Deposit / Withdraw / GetBalance | saved, events sent once in order | rule broken → nothing saved, **no event sent** | unknown account id → `KeyNotFoundException` |

**A bug these tests found:** before, `Deposit(null)`, `Withdraw(null)`, `Add(null)`, `Subtract(null)` and
`IsLessThan(null)` crashed with a `NullReferenceException` from deep inside the method. The NegativeNegative
tests were written first and failed; the methods now start with `ArgumentNullException.ThrowIfNull(...)`,
which names the bad argument straight away.

## The DDD pieces

- **Value object** — `MoneyValueObject`: no id, compared by value, can't be changed (every operation returns a new `MoneyValueObject`).
- **Entity / aggregate root** — `BankAccountAggregate`: has an `Id`, and is the only door to change the
  balance. The setter is `private`, so the rules can't be skipped.
- **Domain events** — `AccountOpenedEvent`, `MoneyDepositedEvent`, `MoneyWithdrawnEvent`, `AccountClosedEvent`.
  The account collects them; `BankAccountService` sends them out after saving.
- **Repository interface** — `IBankAccountRepository`, declared in the domain, implemented outside it.
- **Custom exceptions** — `InsufficientFundsException`, `AccountClosedException`. The demo catches
  one with an exception filter (`catch ... when (...)`).

## OOP in this project

- **Encapsulation:** `Balance` has a private setter; only `Deposit`/`Withdraw` change it.
- **Abstraction:** the service talks to `IBankAccountRepository`, not to a concrete store.
- **Polymorphism:** the service works the same with `AccountRepositoryFake` (tests) or
  `InMemoryAccountRepository` (demo).
- **Interface vs abstract class:** we used interfaces because the implementations share no code.
  An abstract class is useful when you also want to share some code between subclasses.

## Why the domain has no database code

1. **The rules are the valuable part.** "You can't withdraw more than you have" is true whether
   accounts live in SQL Server, PostgreSQL or a text file. Keeping the rules separate means
   changing the database never risks breaking them.
2. **Fast, simple tests.** No database to start, no data to clean up. All tests run in
   milliseconds and only fail when a *rule* is broken, not because a server is down.
3. **One place for each rule.** If SQL code and rules were mixed, the same check would end up
   copied in queries, controllers and stored procedures. Here it exists once, in `BankAccountAggregate`.
4. **Dependencies point inward.** The domain defines `IBankAccountRepository`; the outside world
   implements it. The domain project references nothing, so nothing can leak in.

## Why hand-written fakes and not a mocking library

The fakes are about 10 lines each and anyone can read them. A mocking library (like Moq or
NSubstitute) is handy for big interfaces, but here it would just add a dependency and new syntax to learn.

## Dependency Injection: every kind of registration (`Banking.Demo`, Part 2)

```bash
dotnet run --project Banking.Demo
```

| Registration | What DI does | Use it for |
|---|---|---|
| `AddSingleton<T>()` | Creates **one** object, hands the same one to everyone, forever | Shared, thread-safe things: in-memory stores, caches, settings |
| `AddScoped<T>()` | One object **per scope**. In ASP.NET Core every web request is a scope | Things that belong to one request: EF Core's `DbContext` |
| `AddTransient<T>()` | A **new** object every time it's asked for | Small classes that hold no data: `BankAccountService`, `ReportBuilder` |
| `AddSingleton<T>(serviceProvider => new T(...))` | Calls **your** code to create it | When the constructor needs values DI can't guess |
| `AddSingleton(existingObject)` | Hands out the object **you** already made | Settings objects, test doubles |
| Same interface registered twice | `GetServices<T>()` returns **all** of them; `GetRequiredService<T>()` returns the **last** | Plug-ins, several event handlers |
| `TryAddSingleton<T>()` | Registers only if **nothing** is registered for `T` yet | Libraries: give a default without overwriting the app's choice |
| `AddKeyedSingleton<T>("key")` | Several implementations told apart by a **key** | See Project 7.3 (`"smtp"` / `"trap"` email senders) |
| `AddHostedService<T>()` | A singleton that runs in the background for the app's whole life | See Project 7.3 |
| `AddDbContext` / `AddDbContextPool` | Registers a `DbContext` as **scoped** (pooled: reused between requests) | See Project 6.1 |

The demo prints object ids from two scopes, so you can **see** the lifetimes:

```
scope 1:  Singleton 598803 598803   Scoped ab1d33 ab1d33   Transient df982f 188e20
scope 2:  Singleton 598803 598803   Scoped fd0a8f fd0a8f   Transient 49ea17 80ab53
```

Singleton: same id everywhere. Scoped: same inside a scope, different between scopes. Transient: new every time.

**The classic mistake — "captive dependency":** a **singleton** that needs a **scoped** service. The singleton
lives forever, so it keeps the first scoped object forever (for example one `DbContext` shared by every web
request, which breaks). The demo builds the provider with `ValidateOnBuild = true` and `ValidateScopes = true`,
so the mistake is caught at startup:
`Cannot consume scoped service 'ScopedExample' from singleton 'SingletonThatNeedsScoped'.`
(ASP.NET Core turns these checks on automatically in Development.)

**Rule of thumb:** a service may only depend on services that live **as long or longer**:
Transient → can use anything. Scoped → Scoped or Singleton. Singleton → Singleton only.

**Lifetimes used in Part 1:** the repository and dispatcher are singletons (one shared store, or the data
would disappear); `BankAccountService` is transient (it holds no data, so a fresh one is cheap and safe).

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `record MoneyValueObject` | A value object: `10 USD` equals any other `10 USD` (records compare by value). Get-only properties mean it can't change; `Add` returns a *new* `MoneyValueObject`. | A `class` compares by reference (two `10 USD` objects would be "different") and we'd have to write `Equals`/`GetHashCode` ourselves. |
| `record` not `struct` for `MoneyValueObject` | Every `MoneyValueObject` must go through the constructor checks. | A struct has a `default` value with `Currency = null` that skips the constructor — an invalid `MoneyValueObject` would exist. |
| `class BankAccountAggregate` | An entity: it has an `Id` and changes over time. Two accounts with the same balance are still different accounts. | A record's value equality is wrong for something with identity. |
| Private constructor + `static Open(...)` | The only way to create an account also validates the owner and raises `AccountOpenedEvent`. | A public constructor could create an account without the event. |
| `Balance { get; private set; }` | Readable by anyone, changeable only by `Deposit`/`Withdraw` — so the rules can't be skipped (encapsulation). | A public setter lets anyone write `account.Balance = new MoneyValueObject(1000000, "USD")`. |
| `IReadOnlyList<IDomainEvent> PendingEvents` over a private `List` | Outside code can read the events (with `Count`, index), but can't add or remove. | Exposing the `List` lets anyone fake events. `IEnumerable` would hide `Count`, which tests use. |
| Events: `record` + `interface IDomainEvent` | Each event is an unchangeable fact (`record`). The interface lets different event types share one list. | An `abstract class` base would also work, but there's no shared code to put in it. |
| `interface IBankAccountRepository`, `IDomainEventDispatcher` | Two implementations each (test fake + demo in-memory), and later a SQL one. The domain only knows the interface. | Using the concrete class would tie the domain to one storage. |
| `class BankAccountService` (no interface) | Only one implementation exists. | An `IBankAccountService` would be code with no second user. |
| Exceptions as `class ... : Exception` | C# requires exceptions to inherit `Exception`. Custom types let callers catch exactly *this* problem; extra properties (`Balance`, `Requested`) carry the details. | A plain `InvalidOperationException` can only be told apart by its message text. |
| `Guid` ids | Created in memory, unique everywhere, no database needed. | `int` ids usually come from the database, which the domain must not depend on. |
| `DateTimeOffset HappenedAt` | An exact moment, unambiguous on any server. | `DateTime` can be unclear about local vs UTC. |
| `decimal` amounts, rounded to 2 places | Money. | — |
| Test fakes use `Dictionary` and `List` | `Dictionary` finds accounts by id; `List` keeps events in order so the test can check the order. | — |
