# Project 6.6 — EF Core Advanced Queries

Six things EF Core can do beyond plain `Where(...).ToList()` and `SaveChanges()`, in one small
product table, with tests that run the **real SQL**.

```bash
cd EfCoreAdvancedQueries
dotnet run
```

The demo prints every SQL statement EF sends (`SQL> ...`), so you can see what each technique really does.

```bash
cd EfCoreAdvancedQueries.Tests
dotnet test
```

## Files

| File | What it is |
|---|---|
| `Models/ProductModel.cs` | The product row. `Version` is the concurrency token. |
| `DbContexts/ShopDbContext.cs` | The table, `IsConcurrencyToken()`, and 6 seed products. |
| `Services/ProductQueryService.cs` | All six techniques, one method each. |
| `Outcomes/PriceUpdateOutcome.cs` | `Updated` / `NotFound` / `ConflictSomeoneElseChangedIt`. |
| `Program.cs` | The demo. |
| `EfCoreAdvancedQueries.Tests/Helpers/TestDatabaseHelper.cs` | A fresh in-memory SQLite database per test. |
| `EfCoreAdvancedQueries.Tests/ProductQueryServiceTest.cs` | The tests. |

## The six techniques

| # | Method | EF feature | Why |
|---|---|---|---|
| 1 | `SearchByNameAsync` | `FromSql($"...")` | Write the SQL yourself when LINQ can't express it. Every `{value}` becomes a **parameter** (`@p0`), so SQL injection is impossible. |
| 2 | `RestockAsync` | `Database.ExecuteSqlAsync($"...")` | Raw SQL that changes data and returns no rows. Same parameter safety. |
| 3 | `RaisePricesAsync` | `ExecuteUpdateAsync` | **One** `UPDATE` statement for any number of rows. No loading, no `SaveChanges`. |
| 4 | `DeleteDiscontinuedAsync` | `ExecuteDeleteAsync` | **One** `DELETE` statement. |
| 5 | `GetByIdAsync` | `EF.CompileAsyncQuery` | LINQ → SQL translation done **once**, not on every call. For tiny queries that run all the time. |
| 6 | `UpdatePriceAsync` | concurrency token + `DbUpdateConcurrencyException` | Two people edit the same product: the second save is **refused** instead of silently wiping out the first. |

### Why `FromSql` and not `FromSqlRaw`

```csharp
// SAFE: {pattern} is sent as a parameter. The SQL text is always "... LIKE @p0".
database.Products.FromSql($"SELECT * FROM Products WHERE Name LIKE {pattern}");

// DANGEROUS: the text is glued into the SQL. "' OR 1=1 --" would return every row.
database.Products.FromSqlRaw("SELECT * FROM Products WHERE Name LIKE '" + pattern + "'");
```

`FromSql` only accepts an interpolated string (`$"..."`), and turns each hole into a parameter. Use it.

### Why `ExecuteUpdate` / `ExecuteDelete`

The normal way — load the rows, change them, `SaveChanges()` — sends 1 `SELECT` + 1 `UPDATE` **per row**.
`ExecuteUpdate` sends one statement no matter how many rows match.

**The catch:** it skips the change tracker. A product you already loaded into the same `DbContext`
keeps its **old** values until you reload it (there's a test showing exactly this).

### How the concurrency token works

`Version` is marked `.IsConcurrencyToken()`. EF then writes every update like this:

```sql
UPDATE Products SET PriceInCents = @p0, Version = @p1 WHERE Id = @p2 AND Version = @p3
```

`@p3` is the version the user **saw**. If someone else saved in the meantime, the version has moved on,
**0 rows** match, and EF throws `DbUpdateConcurrencyException`. The service catches it and returns
`ConflictSomeoneElseChangedIt` — the app can then show "someone changed this, please reload".

Every write in this project (including the bulk ones) adds 1 to `Version`, so a bulk price raise also
makes an open edit screen get a conflict.

(SQL Server has a built-in `rowversion` column that changes itself — `[Timestamp] byte[]`. SQLite has
none, so here the code bumps `Version` by hand.)

## Code snippets

**Raw SQL query, safe from injection**, from `Services/ProductQueryService.cs`

```csharp
return database.Products
    .FromSql($"SELECT * FROM Products WHERE Name LIKE {pattern}") // {pattern} becomes @p0
    .OrderBy(product => product.Name)
    .AsNoTracking()
    .ToListAsync();
```

**Raw SQL command**

```csharp
return database.Database.ExecuteSqlAsync(
    $"UPDATE Products SET Stock = Stock + {extraUnits}, Version = Version + 1 WHERE Category = {category} AND IsDiscontinued = 0");
```

**Bulk update and bulk delete: one statement, no loading**

```csharp
return database.Products
    .Where(product => product.Category == category && !product.IsDiscontinued)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(product => product.PriceInCents, product => product.PriceInCents * (100 + percent) / 100)
        .SetProperty(product => product.Version, product => product.Version + 1));

return database.Products
    .Where(product => product.Category == category && product.IsDiscontinued)
    .ExecuteDeleteAsync();
```

**Compiled query: translated to SQL once**

```csharp
private static readonly Func<ShopDbContext, int, Task<ProductModel?>> findByIdCompiled =
    EF.CompileAsyncQuery((ShopDbContext context, int id) => context.Products.AsNoTracking().FirstOrDefault(product => product.Id == id));

return findByIdCompiled(database, id);
```

**Concurrency token: refuse to overwrite someone else's change**

```csharp
product.Property(p => p.Version).IsConcurrencyToken(); // in ShopDbContext.OnModelCreating

database.Entry(product).Property(existing => existing.Version).OriginalValue = versionTheUserSaw;
product.PriceInCents = newPriceInCents;
product.Version = versionTheUserSaw + 1;
try
{
    await database.SaveChangesAsync();
    return PriceUpdateOutcome.Updated;
}
catch (DbUpdateConcurrencyException)
{
    database.ChangeTracker.Clear();
    return PriceUpdateOutcome.ConflictSomeoneElseChangedIt;
}
```

**Print every SQL statement EF sends** (simplified from `Program.cs`, which also tidies each line)

```csharp
.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
```

**Testing against a real in-memory SQLite database**, from `EfCoreAdvancedQueries.Tests/Helpers/TestDatabaseHelper.cs`

```csharp
connection = new SqliteConnection("Data Source=:memory:");
connection.Open(); // the database lives as long as this connection is open

DbContextOptions<ShopDbContext> options = new DbContextOptionsBuilder<ShopDbContext>()
    .UseSqlite(connection)
    .Options;
using ShopDbContext context = new ShopDbContext(options);
context.Database.EnsureCreated();
```

## Variations

Other ways to write the same queries, in `EfCoreAdvancedQueries/Variations/ProductQueryVariation.cs` — two of them
wrong on purpose. `EfCoreAdvancedQueries.Tests/VariationsTest.cs` (8 tests) shows exactly what happens.

**Searching by name**

| Way | `"Mo"` | `"' OR 1=1 --"` | `"O'Brien"` | The difference |
|---|---|---|---|---|
| `FromSql($"... LIKE {pattern}")` (original) | Monitor, Mouse | nothing | nothing | `{pattern}` becomes a parameter |
| `FromSqlInterpolated($"...")` | same | nothing | nothing | older name for the same thing |
| `FromSqlRaw("... LIKE @pattern", new SqliteParameter(...))` | same | nothing | nothing | for SQL text built at runtime; values still parameters |
| plain LINQ `Where(p => p.Name.Contains(text))` | same | nothing | nothing | no SQL written; the usual choice |
| `FromSqlRaw("... LIKE '%" + text + "%'")` ❌ | same | **all 6 products** (SQL injection) | **`SqliteException`** (the quote breaks the SQL) | text joined into SQL |

EF's analyzer warns (`EF1002`) if you pass an **interpolated** string to `FromSqlRaw`; joining with `+` gives **no warning**
(both checked).

**Raising prices for a category**

| Way | Statements | Objects already loaded | Goes through `SaveChanges` |
|---|---|---|---|
| `ExecuteUpdateAsync` (original) | 1 `UPDATE` | **stale** (still the old price) | no — skips overrides, interceptors on `SaveChanges`, concurrency checks |
| load, change, `SaveChangesAsync` | 1 `SELECT` + 1 `UPDATE` per row | up to date | yes |

**Two people changing the same price** (Alice saves 29.99, then Bob saves 19.99, both having seen version 0)

| Way | Bob's result | Final price | The difference |
|---|---|---|---|
| concurrency token + `DbUpdateConcurrencyException` (original) | `ConflictSomeoneElseChangedIt` | 29.99 | EF adds `AND Version = @seen` itself |
| `ExecuteUpdate` with `Version == seen` in the `Where` | 0 rows changed | 29.99 | the same check by hand, without loading |
| `ExecuteUpdate` **without** a version check ❌ | 1 row changed, no error | **19.99** | "last write wins": Alice's change is silently lost |

Not coded (model changes): `[ConcurrencyCheck]` on any property makes that property a token; `[Timestamp] byte[]` is a
`rowversion` column SQL Server updates by itself (SQLite has no such type, hence the manual `Version` here).

## Why these types

| Choice | Why |
|---|---|
| `long PriceInCents` instead of `decimal Price` | SQLite has no exact decimal type. Whole cents in an integer are exact, and SQL can do math on them (the bulk price raise needs `PriceInCents * 110 / 100` in the database). 6.2 stores decimal as text instead, which SQL can't multiply. |
| `int Version` | A simple counter: easy to read in the demo output and in tests. |
| `enum PriceUpdateOutcome` | Three possible results, named. A `bool` can't say "not found" vs "conflict". |
| `Task<int>` from the bulk methods | The number of rows changed — what the database itself reports. |
| `static readonly Func<...>` for the compiled query | Compiled once when the class is first used, shared by every call. |
| `AsNoTracking()` on reads | Read-only results don't need change tracking: less memory, faster. |
| SQLite **in-memory** in tests, not EF's `InMemory` provider | The InMemory provider is not a real database: it can't run `FromSql`/`ExecuteSql` and ignores concurrency tokens. In-memory SQLite runs the real SQL and is still fast. |

## Tests

Every behaviour has a **PositivePositive** (valid input → works), **PositiveNegative** (well-formed input
that breaks a rule or matches nothing → refused / nothing changes) and **NegativeNegative**
(broken or missing input → exception, nothing changes).

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `FromSql` search | "Mo" → Monitor, Mouse | `' OR 1=1; DROP TABLE Products; --` → 0 results, table intact | `null` |
| `ExecuteSql` restock | Furniture +5 → Desk 7, discontinued chair untouched | unknown category → 0 rows | 0 / negative units, empty category |
| `ExecuteUpdate` raise prices | Computers +10% → 3 rows, versions bumped | discontinued + other categories untouched; already-loaded object stays stale until `Reload` | 0%, 101%, empty category |
| `ExecuteDelete` | deletes only discontinued Computers | category with none → 0 deleted | empty / whitespace / null category |
| Compiled query | id 1 → Keyboard | id 999 → `null` | id 0 / negative |
| Concurrency token | current version → saved | Alice then Bob (two DbContexts) → Bob gets conflict; bulk update in between → conflict; unknown id → `NotFound` | price 0 / negative |
