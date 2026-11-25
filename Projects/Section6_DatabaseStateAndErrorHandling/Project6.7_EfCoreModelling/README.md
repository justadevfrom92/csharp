# Project 6.7 — EF Core Modelling (DDD aggregates)

How to map a **clean domain model** with EF Core when the classes shouldn't be shaped by the database:

- **Configuration classes** (`IEntityTypeConfiguration<T>`) instead of one long `OnModelCreating`.
- **Inheritance**: `MaterialModel` and `ServiceModel` both extend `ProductModel` and share **one** table with a `Type` column.
- **Shadow properties**: the database key (`Id`) and foreign key (`InvoiceId`) exist only in the database; the classes
  use a `PublicId` Guid instead.
- **An aggregate**: `InvoiceModel` keeps its lines in a **private** list that EF fills, loads them automatically
  (`AutoInclude`), and only lets outside code add lines through `AddLine`.
- **A reporting query** that runs entirely in SQL: `OfType` → `Join` → `GroupBy` into a `required` record.

```bash
cd EfCoreModelling
dotnet run
```

```bash
cd EfCoreModelling.Tests
dotnet test
```

Output (SQL shortened):

```
1. One table for both kinds (table-per-hierarchy), with a Type column:
   1  Material  Cement
   2  Material  Cable
   3  Service   Installation
2. Material sold, all computed in the database:
    SQL> Executed DbCommand (5ms) [Parameters=[], CommandType='Text', CommandTimeout='30'] SELECT "p"."PublicId" AS "ProductId", "p"."Name", COALESCE(ef_sum("i"."Amount"), '0.0') AS "AmountSold" FROM "Products" AS "p" INNER JOIN "InvoiceLines" AS "i" ON "p"."PublicId" = "i"."ProductId" WHERE "p"."Type" = 'Material' GROUP BY "p"."PublicId", "p"."Name" ORDER BY "p"."Name"
   Cable: 40.0
   Cement: 35.0
3. Loading one invoice - AutoInclude brings its lines without Include():
    SQL> Executed DbCommand (0ms) [Parameters=[@publicId='?' (DbType = Guid)], CommandType='Text', CommandTimeout='30'] SELECT "i1"."Id", "i1"."Customer", "i1"."PublicId", "i0"."Id", "i0"."Amount", "i0"."InvoiceId", "i0"."ProductId" FROM ( SELECT "i"."Id", "i"."Customer", "i"."PublicId" FROM "Invoices" AS "i" WHERE "i"."PublicId" = @publicId LIMIT 1 ) AS "i1" LEFT JOIN "InvoiceLines" AS "i0" ON "i1"."Id" = "i0"."InvoiceId" ORDER BY "i1"."Id"
   Acme: 3 lines
4. A list page with IgnoreAutoIncludes - no lines loaded:
    SQL> Executed DbCommand (0ms) [Parameters=[], CommandType='Text', CommandTimeout='30'] SELECT "i"."Id", "i"."Customer", "i"."PublicId" FROM "Invoices" AS "i"
   Acme: 0 lines loaded
   Globex: 0 lines loaded
5. Lines can only be added through the invoice:
   invoice.Lines is read-only -> NotSupportedException
```

## What's in it

| What | File |
|---|---|
| `abstract ProductModel`, `sealed MaterialModel` / `ServiceModel` — no `Id` property | `EfCoreModelling/Models/` |
| `InvoiceModel`: private `linesCollection`, read-only `Lines`, `AddLine` with rules | `EfCoreModelling/Models/InvoiceModel.cs` |
| `ProductConsumptionModel`: `required` + `init` record | `EfCoreModelling/Models/` |
| shadow key + `HasDiscriminator("Type")` | `EfCoreModelling/Configurations/ProductConfiguration.cs` |
| `HasMany<InvoiceLineModel>("linesCollection")`, shadow `InvoiceId`, `Navigation(...).AutoInclude()` | `EfCoreModelling/Configurations/InvoiceConfiguration.cs` |
| `ApplyConfiguration(new ...)` for each | `EfCoreModelling/DbContexts/ShopDbContext.cs` |
| the "material sold" query, `AutoInclude`, `IgnoreAutoIncludes` | `EfCoreModelling/Services/ProductSalesService.cs` |

## Things the tests showed

- **Everything runs in ONE SQL command**: `OfType<MaterialModel>()` became `WHERE "p"."Type" = 'Material'`, the `Join` an
  `INNER JOIN`, `GroupBy` a `GROUP BY`, and the decimal `Sum` EF's own `ef_sum(...)` (SQLite has no decimal type).
- **`AutoInclude` adds a `LEFT JOIN "InvoiceLines"` to every invoice query.** Good for an aggregate you always use whole;
  wasteful for a list page — hence `IgnoreAutoIncludes()`.
- **`IgnoreAutoIncludes` isn't enough on its own**: when the same `DbContext` had already loaded Acme's lines, a later
  "no lines" list still showed **3 lines for Acme** (and 0 for Globex) — EF attaches tracked objects to each other. The list
  page uses `AsNoTracking()` too (`TrackedListVariation` shows the problem).
- **`EF.Property<T>(...)` works only inside a query.** On a loaded object it throws `InvalidOperationException`; there you
  use `database.Entry(invoice).Property<int>("Id").CurrentValue`.
- **A row with an unknown `Type`** (`'Gadget'`, inserted by hand) makes loading products throw: there's no class for it.
- **Without `AutoInclude`, lines are silently empty** (`InlineModelDbContextVariation`) — no error, the invoice just looks
  empty. `Include("linesCollection")` fixes it; `Include("Lines")` (a wrong name) only fails when the query runs.
- **Returning the private `List` as `IEnumerable` leaks**: it can be cast back and given a -50 line that `AddLine` would
  refuse (`LeakyInvoiceVariation`). `AsReadOnly()` throws `NotSupportedException` on `Add` and can't be cast back.
- **A table is named after its `DbSet`**: the table-per-type context has no `Invoices` DbSet, so the table became
  `InvoiceModel`. Add the DbSet or `ToTable("Invoices")` (6.1 does the same for `AuthorProfile`).
- Each material/service row leaves the other kind's columns `NULL` (`Unit` / `HourlyRate`): one table means nullable columns.

## Code snippets

**A configuration class, applied in the DbContext**

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<ProductModel>
{
    public void Configure(EntityTypeBuilder<ProductModel> builder)
    {
        builder.Property<int>("Id").ValueGeneratedOnAdd(); // shadow key
        builder.HasKey("Id");
        builder.HasDiscriminator<string>("Type")
            .HasValue<MaterialModel>("Material")
            .HasValue<ServiceModel>("Service");
    }
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfiguration(new ProductConfiguration());
    // or: modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopDbContext).Assembly);
}
```

**A private collection, shadow foreign key, always loaded**

```csharp
private readonly List<InvoiceLineModel> linesCollection = [];
public IReadOnlyList<InvoiceLineModel> Lines
{
    get
    {
        return linesCollection.AsReadOnly();
    }
}

builder.HasMany<InvoiceLineModel>("linesCollection").WithOne().HasForeignKey("InvoiceId");
builder.Ignore(invoice => invoice.Lines);
builder.Navigation("linesCollection").AutoInclude();

database.Invoices.IgnoreAutoIncludes().AsNoTracking().ToListAsync(); // a list page without lines
```

**Reading shadow columns**

```csharp
database.Products.Select(product => new { Id = EF.Property<int>(product, "Id"), Type = EF.Property<string>(product, "Type") });
database.Entry(invoice).Property<int>("Id").CurrentValue; // on a tracked object
```

**OfType + Join + GroupBy, all in SQL**

```csharp
await database.Products.OfType<MaterialModel>()
    .Join(database.InvoiceLines, material => material.PublicId, line => line.ProductId,
        (material, line) => new { Material = material, line.Amount })
    .GroupBy(
        pair => new { pair.Material.PublicId, pair.Material.Name },
        (key, group) => new ProductConsumptionModel
        {
            ProductId = key.PublicId,
            Name = key.Name,
            AmountSold = group.Sum(pair => pair.Amount),
        })
    .ToArrayAsync();
```

## Variations

In `EfCoreModelling/Variations/`, checked by `EfCoreModelling.Tests/VariationsTest.cs`.

**Where the model setup lives**

| Way | Good | Watch out |
|---|---|---|
| a configuration class per entity + `ApplyConfiguration(new ...)` (original) | one small file per entity; the list is visible | add a line for each new entity |
| everything inline in `OnModelCreating` (`InlineModelDbContextVariation`) | fine for a few entities | grows into hundreds of lines |
| `ApplyConfigurationsFromAssembly(...)` (`ScannedConfigurationsDbContextVariation`) | nothing to keep up to date; same model (tested) | also applies configurations meant for *another* DbContext in the same assembly |

(The video nests `InvoiceLineConfiguration` inside `InvoiceConfiguration`; this repo keeps one class per file.)

**How inheritance is stored**

| Strategy | Tables | `OfType<MaterialModel>()` becomes | Watch out |
|---|---|---|---|
| **table-per-hierarchy** `HasDiscriminator` (original, EF's default) | 1: `Products` + `Type` | `WHERE Type = 'Material'` | each kind's columns are `NULL` for the others |
| **table-per-type** `UseTptMappingStrategy()` (`TablePerTypeDbContextVariation`) | `Products` + `Materials` + `Services` | a `JOIN "Materials"` | joins on every query; no `Type` column |
| table-per-concrete-type `UseTpcMappingStrategy()` (not built) | one full table per kind | a `UNION` for the base type | keys must be unique across all the kind tables (not built here, so not tested) |

**Loading the lines**

| Way | Lines loaded | SQL |
|---|---|---|
| `Navigation(...).AutoInclude()` (original) | always | `LEFT JOIN "InvoiceLines"` on every invoice query |
| `IgnoreAutoIncludes().AsNoTracking()` (original list page) | none | no join |
| `IgnoreAutoIncludes()` alone (`TrackedListVariation`) | **whatever this DbContext loaded before** | no join — but earlier-tracked lines get attached |
| no `AutoInclude`, no `Include` (`InlineModelDbContextVariation`) | none, **silently** | no join |
| `Include("linesCollection")` | yes | `LEFT JOIN` |

**Exposing the lines** — `AsReadOnly()` (original) vs `IEnumerable` over the `List` (`LeakyInvoiceVariation`): the second can be
cast back to `List<T>` and changed, skipping `AddLine`'s rules.

**The report query** (`QueryStyleVariation`)

| Way | SQL commands | Rows read |
|---|---|---|
| method syntax `Join` + `GroupBy` (original) | 1 with `GROUP BY` | one per material |
| query syntax `from ... join ... group ... by` | 1, the same SQL | one per material |
| load all materials and lines, then LINQ in memory | 2, no `GROUP BY` | **every** material and **every** line |

All three give the same answer (tested).

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| shadow `int Id` + `Guid PublicId` | small fast database key; the Guid is safe to show in URLs | an `int` in a URL can be guessed (1, 2, 3...); a Guid primary key makes bigger, slower indexes |
| `private List` + `AsReadOnly()` | only `AddLine` can add lines, so its rules always run | a public `List` lets any code skip them |
| `IReadOnlyList<T>` for `Lines` | callers get `Count` and `[index]`, not `Add` | `IEnumerable` hides `Count` and can be cast back |
| `abstract ProductModel` + `sealed` kinds | a product is always a material or a service (see 1.4) | — |
| `required` + `init` record for the result | every value must be set; never changed | a positional record works too; `required` names each value at the call |
| `ToArrayAsync()` | a fixed result that won't grow | `ToListAsync()` when the caller adds to it |
| `decimal Amount` | money and quantities exactly | `double` rounding (see 1.1) |
| `private set` everywhere | only the class changes its data; EF can still set it | public setters let any code change a saved invoice |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| table-per-hierarchy | one `Products` table with `Type` | other kind's columns are `NULL` | unknown `Type` row can't be loaded |
| shadow properties | `Id` and `InvoiceId` read with `EF.Property` in a query | classes have no `Id`; `Entry(...)` reads it | `EF.Property` outside a query throws |
| loading the aggregate | `AutoInclude` brings lines in one `LEFT JOIN` query | list page: no lines even after an invoice was loaded | unknown invoice → `null` |
| adding lines | saved through the invoice | `Lines` is read-only and can't be cast back | amount 0, `null` product, blank customer rejected |
| material report | summed per material in one `GROUP BY` command | services and unsold materials left out | no lines → empty array |
| inline variation | `Include("linesCollection")` loads | no `AutoInclude` → silently empty | wrong `Include` name throws when run |
| scanned / table-per-type | same model; a table per kind | `OfType` becomes a `JOIN` | — |
| leaky list, query styles, tracked list | all query styles equal | in-memory: 2 commands, every row; tracked list shows earlier lines | cast back to `List` skips the rules |
