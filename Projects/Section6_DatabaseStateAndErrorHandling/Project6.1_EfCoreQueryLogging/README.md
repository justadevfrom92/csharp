# Project 6.1 — EF Core with SQL Query Logging

> New to `.cshtml` files (`@model`, `@foreach`, `ViewBag`, layouts)? Read [RAZOR_BASICS.md](../../RAZOR_BASICS.md) first.

An MVC page listing books (with author, author profile and tags). Every SQL command EF runs
is logged — exact SQL, parameters and time — to the console and `logs/app-<date>.log` via Serilog.

## Run

```bash
dotnet run
```
The database (`library.db`, SQLite) is created and migrated on startup.

## Migrations (code-first)

The model classes are in `Models/` (`AuthorModel.cs`, `AuthorProfileModel.cs`, `BookModel.cs`, `TagModel.cs`). After changing them:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add SomeChangeName -o Migrations
```

```bash
dotnet ef database update
```

(`dotnet tool restore` installs `dotnet-ef` from `Projects/dotnet-tools.json`.)

## What it covers

- **Relationships:** Author–AuthorProfile (1:1), Author–Books (1:N), Books–Tags (N:N).
- **Seed data:** `HasData` in `OnModelCreating` — it's part of the first migration.
- **Query logging, two ways:**
  - `SqlQueryLoggingInterceptor` (a `DbCommandInterceptor`) — our own code, sees every command.
  - `EnableSensitiveDataLogging()` (Development only) — shows real parameter values.
  - (`options.LogTo(Console.WriteLine)` is the quickest third option for console apps.)
- **Pooling:** `AddDbContextPool` reuses `DbContext` objects between requests.
- **Retries:** SQLite is a local file, so there's nothing to retry. With SQL Server you'd write
  `options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5))` to retry short network blips.

**Why SQLite instead of SQL Server/PostgreSQL:** it's a single file, so the project runs with
`dotnet run` and no server to install. The EF code is the same; only `UseSqlite` changes to
`UseSqlServer` / `UseNpgsql` and the connection string.

## Why these logging levels

| Environment | Interceptor level | What you see |
|---|---|---|
| Development (`appsettings.Development.json`) | `Debug` | Every SQL statement + parameter values + time |
| Production (`appsettings.json`) | `Warning` | Only slow queries (> 200 ms) and failed queries |

**Why not log every query in production?**
1. **Volume.** A busy site runs thousands of queries a second. Logging all of them fills disks,
   costs money in log services, and slows the app down (writing logs isn't free).
2. **Privacy/security.** Parameters contain emails, names, sometimes password hashes or tokens.
   `EnableSensitiveDataLogging` is only turned on in Development for this reason.
3. **Signal vs noise.** In production you want to know what's *wrong*: slow queries and errors.
   Those are logged at `Warning`/`Error`, so they still show up. Nobody reads a million "all good" lines.

**Why still log slow queries in production:** that's where real performance problems show up first
(missing index, query returning too much data). The SQL text is logged, but not the parameter values.

**Why `Microsoft.EntityFrameworkCore` is set to `Warning`:** EF's own logs repeat what our interceptor
already says. One clear source is easier to read.

**Why the levels live in appsettings and not in code:** you can turn on Debug logging on a live server
by changing config, without rebuilding the app.

## Code snippets

**Register the `DbContext` (pooled) with the SQLite connection string and the interceptor**, from `Program.cs`

```csharp
builder.Services.AddSingleton<SqlQueryLoggingInterceptor>();
builder.Services.AddDbContextPool<LibraryDbContext>((serviceProvider, options) =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("Library"));
    options.AddInterceptors(serviceProvider.GetRequiredService<SqlQueryLoggingInterceptor>());
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(); // real parameter values in logs: NEVER in production
    }
});
```

**Serilog, reading its settings from `appsettings*.json`**

```csharp
builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
});
app.UseSerilogRequestLogging(); // one tidy log line per HTTP request
```

**Relationships: one-to-one and many-to-many**, from `DbContexts/LibraryDbContext.cs`

```csharp
modelBuilder.Entity<AuthorModel>()
    .HasOne(author => author.Profile)
    .WithOne(profile => profile.Author)
    .HasForeignKey<AuthorProfileModel>(profile => profile.AuthorId);

modelBuilder.Entity<BookModel>()
    .HasMany(book => book.Tags)
    .WithMany(tag => tag.Books)
    .UsingEntity(join => join.ToTable("BookTag"));

modelBuilder.Entity<TagModel>().HasIndex(tag => tag.Name).IsUnique();
modelBuilder.Entity<BookModel>().HasData(new BookModel { Id = 1, Title = "Frankenstein", PublishedYear = 1818, AuthorId = 1 });
```

**Loading related data: `Include`, `ThenInclude`, `AsNoTracking`**, from `Controllers/BooksController.cs`

```csharp
List<BookModel> books = await database.Books
    .Include(book => book.Author)
    .ThenInclude(author => author.Profile)
    .Include(book => book.Tags)
    .AsNoTracking() // read-only page: no need for EF to track changes
    .OrderBy(book => book.Title)
    .ToListAsync();
```

**Adding a row with related rows, then saving**

```csharp
List<TagModel> tags = await database.Tags.Where(tag => tagIds.Contains(tag.Id)).ToListAsync();
database.Books.Add(new BookModel { Title = title.Trim(), PublishedYear = publishedYear, Author = author, Tags = tags });
await database.SaveChangesAsync();
```

**An interceptor that sees every SQL command**, from `Interceptors/SqlQueryLoggingInterceptor.cs`

```csharp
public class SqlQueryLoggingInterceptor : DbCommandInterceptor
{
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        LogCommand(command, eventData.Duration);
        return ValueTask.FromResult(result);
    }
}
```

**Migrations**

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

```csharp
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.Migrate();
}
```

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| Entities are `class` with `{ get; set; }` | EF creates them empty, fills properties from the database, and tracks changes to them. | Records are meant to stay unchanged; EF change tracking expects mutable objects. |
| `int Id` | EF's convention: an `int` named `Id` becomes an auto-increment primary key. | `Guid` works too but makes bigger, unordered indexes for no gain here. |
| `List<BookModel> Books { get; set; } = new()` | Navigation collection EF fills; starting with an empty list means code never hits `null`. | `ICollection<T>` is also common; `List` is simpler and EF accepts it. |
| `AuthorProfileModel? Profile` | An author may have no profile yet (the 1:1 is optional). | Non-nullable would claim every author has one. |
| `AuthorModel Author { get; set; } = null!` | Every book *has* an author, but EF sets it after loading. `null!` says "filled in later". | `AuthorModel?` would force null checks on something that's never null once loaded. |
| `DbSet<T> { get; set; } = null!` | EF finds and fills these properties itself. | — |
| `database.Books` is `IQueryable`, ends with `ToListAsync()` | `Where`/`Include`/`OrderBy` on `IQueryable` become SQL. `ToListAsync` runs it **now**, inside the request. | Handing a lazy query to the view would run SQL during rendering — harder to log, and fails if the `DbContext` is gone. |
| `AsNoTracking()` | Read-only page; EF skips the change-tracking bookkeeping. | Tracking costs memory and time for objects we'll never save. |
| `SqlQueryLoggingInterceptor : DbCommandInterceptor` | EF's base class already has empty versions of ~20 methods; we override only the 5 we need. | Implementing `IDbCommandInterceptor` directly means writing all of them. |
| `ValueTask<...>` return types in the interceptor | That's the signature of the base methods we override. | — |
| Interceptor registered as **singleton** | It holds only a logger — safe to share. | Scoped would create one per request for nothing. |
| `const int SlowQueryMilliseconds` | Fixed threshold, known at compile time. | Could move to config if ops need to tune it. |
| `int[] tagIds` action parameter | The model binder fills an array from repeated `tagIds=1&tagIds=2` checkbox values. | — |
