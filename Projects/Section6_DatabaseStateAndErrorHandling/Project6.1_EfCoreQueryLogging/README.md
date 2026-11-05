# Project 6.1 — EF Core with SQL Query Logging

An MVC page listing books (with author, author profile and tags). Every SQL command EF runs
is logged — exact SQL, parameters and time — to the console and `logs/app-<date>.log` via Serilog.

## Run

```bash
dotnet run
```
The database (`library.db`, SQLite) is created and migrated on startup.

## Migrations (code-first)

The model classes are in `Data/LibraryModels.cs`. After changing them:

```bash
dotnet tool restore
```

```bash
dotnet ef migrations add SomeChangeName -o Data/Migrations
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
