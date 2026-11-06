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

## The DDD pieces

- **Value object** — `Money`: no id, compared by value, can't be changed (every operation returns a new `Money`).
- **Entity / aggregate root** — `BankAccount`: has an `Id`, and is the only door to change the
  balance. The setter is `private`, so the rules can't be skipped.
- **Domain events** — `AccountOpened`, `MoneyDeposited`, `MoneyWithdrawn`, `AccountClosed`.
  The account collects them; `BankAccountService` sends them out after saving.
- **Repository interface** — `IBankAccountRepository`, declared in the domain, implemented outside it.
- **Custom exceptions** — `InsufficientFundsException`, `AccountClosedException`. The demo catches
  one with an exception filter (`catch ... when (...)`).

## OOP in this project

- **Encapsulation:** `Balance` has a private setter; only `Deposit`/`Withdraw` change it.
- **Abstraction:** the service talks to `IBankAccountRepository`, not to a concrete store.
- **Polymorphism:** the service works the same with `FakeAccountRepository` (tests) or
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
   copied in queries, controllers and stored procedures. Here it exists once, in `BankAccount`.
4. **Dependencies point inward.** The domain defines `IBankAccountRepository`; the outside world
   implements it. The domain project references nothing, so nothing can leak in.

## Why hand-written fakes and not a mocking library

The fakes are about 10 lines each and anyone can read them. A mocking library (like Moq or
NSubstitute) is handy for big interfaces, but here it would just add a dependency and new syntax to learn.

## DI lifetimes used in the demo

- `AddSingleton` — the repository: one store shared by everyone, or data would disappear.
- `AddTransient` — the service: it holds no data, so a fresh one each time is cheap and safe.
- `AddScoped` (not used here) — one per web request; that's what EF Core's `DbContext` uses in Section 6.

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `record Money` | A value object: `10 USD` equals any other `10 USD` (records compare by value). Get-only properties mean it can't change; `Add` returns a *new* `Money`. | A `class` compares by reference (two `10 USD` objects would be "different") and we'd have to write `Equals`/`GetHashCode` ourselves. |
| `record` not `struct` for `Money` | Every `Money` must go through the constructor checks. | A struct has a `default` value with `Currency = null` that skips the constructor — an invalid `Money` would exist. |
| `class BankAccount` | An entity: it has an `Id` and changes over time. Two accounts with the same balance are still different accounts. | A record's value equality is wrong for something with identity. |
| Private constructor + `static Open(...)` | The only way to create an account also validates the owner and raises `AccountOpened`. | A public constructor could create an account without the event. |
| `Balance { get; private set; }` | Readable by anyone, changeable only by `Deposit`/`Withdraw` — so the rules can't be skipped (encapsulation). | A public setter lets anyone write `account.Balance = new Money(1000000, "USD")`. |
| `IReadOnlyList<IDomainEvent> PendingEvents` over a private `List` | Outside code can read the events (with `Count`, index), but can't add or remove. | Exposing the `List` lets anyone fake events. `IEnumerable` would hide `Count`, which tests use. |
| Events: `record` + `interface IDomainEvent` | Each event is an unchangeable fact (`record`). The interface lets different event types share one list. | An `abstract class` base would also work, but there's no shared code to put in it. |
| `interface IBankAccountRepository`, `IDomainEventDispatcher` | Two implementations each (test fake + demo in-memory), and later a SQL one. The domain only knows the interface. | Using the concrete class would tie the domain to one storage. |
| `class BankAccountService` (no interface) | Only one implementation exists. | An `IBankAccountService` would be code with no second user. |
| Exceptions as `class ... : Exception` | C# requires exceptions to inherit `Exception`. Custom types let callers catch exactly *this* problem; extra properties (`Balance`, `Requested`) carry the details. | A plain `InvalidOperationException` can only be told apart by its message text. |
| `Guid` ids | Created in memory, unique everywhere, no database needed. | `int` ids usually come from the database, which the domain must not depend on. |
| `DateTimeOffset HappenedAt` | An exact moment, unambiguous on any server. | `DateTime` can be unclear about local vs UTC. |
| `decimal` amounts, rounded to 2 places | Money. | — |
| Test fakes use `Dictionary` and `List` | `Dictionary` finds accounts by id; `List` keeps events in order so the test can check the order. | — |
