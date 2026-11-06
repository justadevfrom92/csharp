# Why These Types — the rules used in every project

Each project's README has a **"Why these types"** section that explains the choices made in *that*
project. This page is the general rule behind them.

---

## 1. Collections

| Type | What it is | Use it when | Don't use it when |
|---|---|---|---|
| `T[]` (array) | Fixed size, fast | The number of items never changes (`string[] categories = ["A", "B"]`) | You need to add/remove items |
| `List<T>` | Growable list, keeps order | You build up items, or need a real, finished copy of a result | You need fast "find by key" (it checks items one by one) |
| `Dictionary<TKey, TValue>` | Key → value lookup | You look things up by an id/code all the time (O(1)) | Order matters, or you just loop over everything |
| `HashSet<T>` | Unique items, no order | "Is X in the set?" or "no duplicates allowed" | You need duplicates or order |
| `Queue<T>` | First in, first out | Work handled in arrival order (orders, jobs) | You need random access |
| `Stack<T>` | Last in, first out | Undo, "go back" | Anything fair/first-come |
| `Channel<T>` | Thread-safe async queue | One part of the app produces, another consumes, at the same time | Single-threaded code (a `Queue` is enough) |

### What to *return* or *accept*: `IEnumerable<T>` vs `IReadOnlyList<T>` vs `List<T>`

| Type | Promise it makes | Use it for |
|---|---|---|
| `IEnumerable<T>` | "You can loop over me once." May be **lazy**: a LINQ query that hasn't run yet. | A **parameter** when your method only loops (accepts arrays, lists, queries, anything). Also LINQ pipelines. |
| `IReadOnlyList<T>` | "Finished list, has `Count` and `[index]`, you can't change it." | **Returning** your internal list so callers can read but not add/remove (e.g. `BankAccount.PendingEvents`). |
| `List<T>` | "Finished list, it's yours, change it if you like." | **Returning a fresh copy** (`.ToList()`) that nobody else holds — e.g. the in-memory stores, which copy *inside* a lock. |
| `IQueryable<T>` | "A query that will be **turned into SQL**." | Only EF Core (`database.Books.Where(...)`). Call `ToListAsync()` before handing data to a view. |
| `IAsyncEnumerable<T>` | "Items arrive one by one over time, `await foreach` me." | Reading from a `Channel` or a stream. |

**Why the in-memory stores return `List<T>`, not `IEnumerable<T>`:** they read the shared list
inside a `lock`. A lazy `IEnumerable` would run *after* the method returned — i.e. after the lock
was released — while another thread might be adding to the list. That's a crash
("Collection was modified"). `.ToList()` makes the copy *while* the lock is held.

**Why we don't return `IEnumerable<T>` from EF Core:** same idea. If the query runs later (in the
view), the `DbContext` may already be gone. `ToListAsync()` runs the SQL right there.

---

## 2. `class` vs `record` vs `struct` vs `interface` vs `static class` vs `abstract class`

| Kind | Pick it when | Example in these projects |
|---|---|---|
| `record` | Plain data that **never changes after it's created**, and two values with the same data should count as **equal**. One-line syntax. | `Order`, `Sale` (2.1), `Money` and the domain events (4.2), `EmailMessage` (7.3), `CatalogProduct` (7.4) |
| `class` | The thing **changes over time**, has an **identity** (two accounts with the same balance are still two accounts), or a framework needs settable properties. | `Product` stock (2.1), `BankAccount` (4.2), EF entities (6.x), form models (3.x), controllers, services |
| `struct` | Tiny value (a few numbers) created in huge amounts, where avoiding heap allocations matters. | **Not used.** See note below. |
| `interface` | You need **several interchangeable implementations**, or a fake for tests. Only describes *what*, no code. | `IEmailSender` (3 senders, 7.3), `IBankAccountRepository` (fake + in-memory, 4.2) |
| `abstract class` | Several classes share **real code** and only fill in a few parts. | Not written by us, but we *inherit* framework ones: `BackgroundService`, `ValidationAttribute`, `DbCommandInterceptor`, `ActionFilterAttribute` |
| `static class` | Just functions or app-wide constants, **no state per object**. | `UrlSlugMaker` (4.1), `LogLineParser` (5.2), `FeatureNames` (7.5) |

**Why no `struct`?** A struct always has a hidden "all zeros" value (`default`) that skips your
constructor. For `Money` that would be an amount with a `null` currency — exactly what the
constructor exists to forbid. A `record` (class) can't be created without going through the
constructor. The memory saving of a struct doesn't matter at this scale.

**Why not add an interface for every class?** An interface with only one implementation is extra
code that buys nothing. `BankAccountService` has no `IBankAccountService` because there's only one.
Add the interface the day a second implementation (or a test fake) actually needs it.

**Why entity and form classes aren't records:** EF Core and MVC model binding create the object
empty and then **set properties one at a time**. That needs a mutable `class` with `{ get; set; }`.

---

## 3. Numbers

| Type | Use for | Why |
|---|---|---|
| `int` | Counts, ids, quantities | Fast and big enough (±2.1 billion). |
| `long` | Byte sizes, milliseconds, counters that can pass 2.1 billion | `Stopwatch.ElapsedMilliseconds` and `IFormFile.Length` are `long`. A log pipeline can count past 2.1 billion lines. |
| `decimal` | **Money**, always | Stores base-10 digits exactly: `0.1 + 0.2 == 0.3`. Also always throws on overflow. |
| `double` | Measurements, percentages for display, science | Fast, huge range, but tiny rounding errors (`0.1 + 0.2 = 0.30000000000000004`). Never for money. |

**`checked` / `unchecked`:** by default an `int` that goes past its maximum silently wraps to a
negative number. `checked(...)` makes it throw instead. Use `checked` when a wrong number is dangerous.
Full explanation in Project 1.1's README.

**`1024L * 1024L` (3.3):** the `L` makes it a `long`. Without it, `megabytes * 1024 * 1024` is
`int` math, and 2,048 MB already overflows `int` — the silent wrap from Project 1.1.

---

## 4. Dates and times

| Type | Use for |
|---|---|
| `DateOnly` | A calendar day with no time: payment dates, "activity on 2026-11-05". |
| `TimeOnly` | A clock time with no day: "10:30". |
| `DateTimeOffset` | **An exact moment**, carrying its UTC offset. Events, receipts, "when did it happen". |
| `DateTime` (always UTC, named `...Utc`) | Database columns, because EF Core + SQLite handle it best. We always store UTC and say so in the name (`CreatedAtUtc`). |
| `TimeSpan` | A length of time: timeouts, expiry, "20 minutes". |

---

## 5. Async

| Type | Use for |
|---|---|
| `Task` / `Task<T>` | Any async method. The default. |
| `ValueTask<T>` | Methods that **usually finish without waiting** (e.g. cache hit). Saves an allocation on the fast path. Rule: await it exactly once. |
| `Thread` | Only to learn the basics (5.1) or for a long-lived background loop. Normal code uses `Task`. |
| `async Task<IActionResult>` | Controller actions that do I/O (database, files). The thread is freed while waiting. |

---

## 6. Small keywords

| Keyword | Why |
|---|---|
| `const` | A value fixed at **compile time** (`const string CartSessionKey = "ShoppingCart"`). Required where C# needs a constant, e.g. inside attributes: `[RequireFeatureFlag(FeatureNames.NewCheckout)]`. |
| `static readonly` | A value created once at startup that can't be a `const` (lists, objects). `readonly` stops the *field* being replaced. |
| `readonly` field | Set once in the constructor (injected services), can never be swapped by accident later. |
| `T?` (nullable) | "This can be missing." `Product? FindById` returns `null` for "not found". The compiler then forces callers to check. |
| `= null!` | "The framework fills this in later, trust me" (EF navigation properties, xUnit setup). Used sparingly. |
| `var` | Only when the type is obvious from the right-hand side (`var list = new List<string>()`) or **impossible to write** (anonymous types from LINQ `Select(x => new { ... })`). Otherwise the type is written out so you can read it. |
| `lock (privateObject)` | The lock object is a private `object` used for nothing else, so no outside code can lock on it and cause a deadlock. |
| Fields instead of properties for counters (5.1, 5.2) | `Interlocked.Increment(ref x)` needs `ref` to a real variable; a property is a method, so you can't `ref` it. |

---

## 7. MVC-specific

| Choice | Why |
|---|---|
| `IActionResult` return type | One action can return a `View`, a `Redirect`, or `NotFound` — different classes. The interface covers them all. |
| Strongly-typed `@model` | The main data of a page. The compiler checks property names. |
| `ViewBag` | Small extras (a dropdown list, an error message). It's `dynamic`: **no compile-time checking**, typos only show up at runtime. Fine for a demo; a real app should put these in a view-model class. |
| `TempData` | A message that must survive **one redirect** ("Product added"). `ViewBag` is lost on redirect. |
| `Controller` vs `ControllerBase` | `Controller` adds views. Pure JSON APIs (6.3, 7.3) use `ControllerBase`. |
