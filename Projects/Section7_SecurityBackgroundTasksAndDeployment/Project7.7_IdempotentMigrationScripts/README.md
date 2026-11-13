# Project 7.7 — Zero-Downtime Idempotent Migration Scripts

An EF Core model (Customers, Orders) with two migrations, and the generated **idempotent** SQL
script (`migrations.sql`) that a deployment pipeline runs *before* the new app version starts.

```bash
./generate-migration-script.sh
```

```bash
dotnet run
```

With a real SQL Server, the app checks the schema and refuses to start if a migration is missing:

```bash
ORDERS_CONNECTION_STRING="Server=...;Database=Orders;..." dotnet run
```

(This project uses the SQL Server provider because EF Core can't generate idempotent scripts for
SQLite. Generating the script needs no database at all.)

## What "idempotent" means here

Open `migrations.sql`: every migration is wrapped in
`IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '...')`.
So the *same* file can be run against:
- an empty database → applies both migrations,
- a database at `InitialCreate` → applies only `AddCustomerEmail`,
- an up-to-date database → does nothing.

Running it twice is harmless. The pipeline never has to know which version a database is at.

## Production runbook

**Build stage (CI):**
1. `dotnet ef migrations script --idempotent --output migrations.sql`
2. Save `migrations.sql` as a build artifact next to the app. It's reviewed in the pull request like code.

**Deploy stage:**
1. **Backup** the database (or confirm point-in-time restore is on).
2. **Run `migrations.sql` once**, from the pipeline, with a database account that is allowed to change
   the schema (`sqlcmd -S <server> -d Orders -i migrations.sql`). Stop the deployment if it fails.
3. **Roll out the new app version**, one server at a time. The app account only has read/write data
   rights — it *can't* change the schema.
4. Each new server checks `GetPendingMigrations()` at startup and exits with an error if the script
   wasn't run, so a missed step is caught immediately instead of causing random SQL errors.
5. Watch errors/health checks. **Rollback** = redeploy the old app version (it still works, see rule below).

**Rule for zero-downtime changes ("expand, then contract"):** during step 3, old and new app versions run
at the same time against the *new* schema. So each migration must work for both:
- **Expand** (this release): only *add* things — new tables, new **nullable** columns (like `Email`).
  The old version simply ignores them.
- **Contract** (a later release, once no old version is running): remove or rename old columns, make
  columns required.
A rename is done as add-new → copy data → (next release) drop-old, never as one step.

## Why NOT `Database.Migrate()` at startup on multiple servers

1. **Race condition.** Five servers start together and all try to apply the same migration. Best case,
   four of them fail and crash-loop; worst case, half-applied changes. (EF Core 9+ takes a lock to
   reduce this, but the other problems remain.)
2. **Too many permissions.** To migrate, the app's database account needs rights to `CREATE`/`ALTER`/`DROP`
   tables. If the app is ever hacked (e.g. SQL injection), the attacker gets those rights too. With
   scripts, the app runs with data-only rights.
3. **No review, no control over timing.** A migration that rebuilds a big table can lock it for minutes.
   With startup migration it runs whenever a server happens to restart — maybe at peak time. A script
   is reviewed first and run in a planned window.
4. **Failures in the wrong place.** If the migration fails, *the app doesn't start*: your outage is now
   a schema problem *and* a down website. With a separate step, a failed script stops the deployment
   while the old version keeps serving traffic.
5. **Startup gets slow and unpredictable**, which breaks health checks and auto-scaling.

`Migrate()` at startup is fine for local development and single-instance demos — that's why Project 6.1 uses it.

## Code snippets

**Generate the idempotent SQL script** (safe to run any number of times), from `generate-migration-script.sh`

```bash
dotnet tool restore
dotnet ef migrations script --idempotent --output migrations.sql
```

**What "idempotent" looks like in the script**: every step checks whether it already ran, from `migrations.sql`

```sql
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106065446_InitialCreate'
)
BEGIN
    ...
END;
```

**Let `dotnet ef` build the `DbContext` without running the app**, from `Factories/DesignTimeDbContextFactory.cs`

```csharp
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlServer("Server=localhost;Database=Orders;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new OrdersDbContext(options);
    }
}
```

**The app never changes the schema; it refuses to start if the database is behind**, from `Program.cs`

```csharp
List<string> pendingMigrations = database.Database.GetPendingMigrations().ToList();
if (pendingMigrations.Count > 0)
{
    Console.WriteLine($"STOP: the database is missing {pendingMigrations.Count} migration(s)");
    return 1; // non-zero exit = deployment fails loudly
}
```

**Money columns in SQL Server**, from `DbContexts/OrdersDbContext.cs`

```csharp
modelBuilder.Entity<OrderModel>().Property(order => order.Total).HasPrecision(18, 2); // decimal(18,2)
```

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| Entities are `class` | Same reasons as 6.1. | — |
| `string? Email` (nullable) | Old app versions don't know this column; they must still be able to insert rows during a rolling deploy. | A required column would make old servers' inserts fail. |
| `decimal` + `HasPrecision(18, 2)` | Money; SQL Server needs to know the size of the column. | Without it EF warns and picks a default that may cut digits. |
| `DesignTimeDbContextFactory : IDesignTimeDbContextFactory<T>` | The `dotnet ef` tool looks for this interface to create the context without running the app. | — |
| `return 0` / `return 1` from top-level code | The program's exit code. A deploy pipeline checks it: non-zero = stop. | Printing an error but exiting with 0 would let a broken deploy continue. |
| `List<string> pendingMigrations` | We need `Count` and then the names — `ToList()` runs the check once. | A lazy `IEnumerable` would query the database twice. |
| `string? connectionString` | Missing environment variable → `null` → print help instead of crashing. | — |
