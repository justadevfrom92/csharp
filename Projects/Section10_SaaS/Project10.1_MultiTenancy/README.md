# Project 10.1 — Multi-Tenancy (one app, many customers, no data leaks)

The first SaaS building block. **Tenants** are customer companies (`acme`, `globex`) sharing one app and one
database. Each request says which tenant it is for; the database layer makes sure it **only ever sees and
changes that tenant's rows**, without every query having to remember.

```bash
cd MultiTenancy
dotnet run
```

```bash
cd MultiTenancy.Tests
dotnet test
```

Try it (port 5101):

```bash
curl -H "X-Tenant: acme" http://localhost:5101/projects
```

```bash
curl -H "X-Tenant: globex" http://localhost:5101/projects
```

```
acme   -> [{"id":1,"tenantId":"acme","name":"Website"},{"id":2,"tenantId":"acme","name":"Mobile app"}]
globex -> [{"id":3,"tenantId":"globex","name":"Rocket"}]
no header -> 400 "Missing X-Tenant header."      acme asking for /projects/3 -> 404
```

## How it works

```
request ── X-Tenant: acme ──► TenantResolutionMiddleware ──► TenantContext (scoped: this request = acme)
                                   │ no header → 400                     │
                                   │ unknown   → 404                     ▼
                                                     AppDbContext ── reads:  global query filter  WHERE TenantId = 'acme'
                                                                 └── writes: SaveChanges stamps TenantId = 'acme',
                                                                             refuses to change another tenant's row
```

| Piece | Job | File |
|---|---|---|
| `TenantResolutionMiddleware` | read the tenant from the request; stop with 400/404 | `Middlewares/` |
| `TenantContext` | holds "this request's tenant" (one per request) | `Contexts/` |
| `TenantStore` | the list of customers | `Stores/` |
| `AppDbContext` | global query filter + tenant stamping on save | `DbContexts/` |
| `ProjectService` | normal queries, **no** `Where(TenantId ...)` anywhere | `Services/` |
| endpoints | `GET/POST /projects`, `GET/PUT/DELETE /projects/{id}`, `GET /me` | `Endpoints/` |

## Ways to separate tenants' data

| Model | How | Good | Bad |
|---|---|---|---|
| **shared database, `TenantId` column** (this project) | one table for everyone + a filter | cheap, one migration, easy to add tenants | one bug can leak everyone's data, so you need the filter and tests |
| schema per tenant | `acme.Projects`, `globex.Projects` | stronger separation | migrations × N schemas |
| **database per tenant** (`DatabasePerTenantVariation`) | a connection string per tenant | strongest: other rows aren't there; easy to export or delete one customer | N databases to migrate, back up and pay for |

Many SaaS apps mix them: shared for small customers, a dedicated database for big ones.

## Things the tests showed

- **A tenant that was never set sees nothing**, not everything: the filter compares with `""`. Saving with no tenant throws.
- **Another tenant's id behaves like a missing one**: `GET/PUT/DELETE /projects/3` as acme → **404**. A 403 would tell an attacker the id exists.
- **`FromSql($"SELECT * FROM Projects")` is still filtered** (EF wraps it); acme got 2 rows, not 3.
- **`ExecuteDelete` is filtered**: acme deleting id 3 removed 0 rows.
- **`Database.ExecuteSqlAsync("UPDATE ...")` is NOT filtered**: acme renamed globex's project. Raw SQL commands must add the tenant themselves.
- A `tenantId` sent in the JSON body is **ignored** (`SaveProjectRequestModel` has no such field), and `SaveChanges` overwrites a `TenantId` set in code.
- Changing a row's `TenantId`, or saving another tenant's row loaded with `IgnoreQueryFilters()`, **throws**.
- **The filter written wrong leaks** (`CapturedFilterDbContextVariation`): copying the tenant id into a local variable inside
  `OnModelCreating` baked in the first tenant, because EF builds the model **once** and caches it. Globex then got **acme's** rows.

## Code snippets

**The global query filter**, from `DbContexts/AppDbContext.cs`

```csharp
public string CurrentTenantId
{
    get
    {
        return tenantContext.TenantId; // a property OF THE DbContext: read again for every query
    }
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<ProjectModel>().HasQueryFilter(project => project.TenantId == CurrentTenantId);
    modelBuilder.Entity<ProjectModel>().HasIndex(project => project.TenantId);
}
```

**Stamping writes**

```csharp
if (entry.State == EntityState.Added)
{
    entry.Entity.TenantId = CurrentTenantId;
}
else if (entry.Entity.TenantId != CurrentTenantId || entry.Property(project => project.TenantId).IsModified)
{
    throw new InvalidOperationException("Changing another tenant's data is not allowed.");
}
```

**Reading the tenant in middleware** (scoped services go in `InvokeAsync`, not the constructor)

```csharp
public async Task InvokeAsync(HttpContext context, TenantStore store, TenantContext tenantContext)
{
    string? tenantId = context.Request.Headers["X-Tenant"];
    ...
    tenantContext.Set(tenant);
    await next(context);
}
```

**Registration**

```csharp
builder.Services.AddScoped<TenantContext>();     // one per request
app.UseMiddleware<TenantResolutionMiddleware>(); // before the endpoints
```

**Admin-only: turn the filter off**

```csharp
database.Projects.IgnoreQueryFilters().CountAsync();
```

## Variations

In `MultiTenancy/Variations/`, checked by `MultiTenancy.Tests/VariationsTest.cs`.

**Where the tenant comes from** (`TenantLookupVariation`)

| Source | Example | The difference |
|---|---|---|
| `X-Tenant` header (original) | `X-Tenant: acme` | simple to test; **the client chooses it**, so in production you must check the user belongs to that tenant |
| **claim in the login token** (`FromClaims`) | JWT `"tenant": "acme"` (8.2) | safest: changing it breaks the signature |
| subdomain (`FromHost`) | `acme.myapp.com` | each customer has its own address; needs wildcard DNS and certificate; `www`, the main domain and `a.b.myapp.com` give `null` |
| URL path (`FromPath`) | `/t/acme/projects` | easy links; every route needs the prefix |

**How the data is kept apart**

| Way | Result in the tests |
|---|---|
| global query filter on a DbContext property (original) | each tenant sees only its rows |
| `Where(TenantId == ...)` written by hand (`ManualFilterVariation.ListWithWhere`) | works... |
| ...and one query that forgot it (`ListForgotWhere`) | **every tenant's rows** |
| trusting a `TenantId` sent by the client (`CreateTrustingTheClient`) | an acme user **planted a row in globex** |
| filter capturing a local variable (`CapturedFilterDbContextVariation`) | **the first tenant baked in**: globex saw acme's data |
| database per tenant (`DatabasePerTenantVariation`) | separate by design; even a forgotten `Where` can't leak; a new tenant starts empty |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `TenantContext` registered **scoped** | one per request; two requests never share it | singleton: requests would overwrite each other's tenant |
| `string TenantId` (`"acme"`) | readable in URLs, logs and headers | an `int` id is guessable and meaningless in a subdomain |
| `""` when no tenant | matches no rows: a forgotten tenant fails **closed** | `null` would make the comparison odd; "all rows" would fail open |
| `TenantModel` as a `record` | made once, never changed | — |
| `ProjectModel` as a mutable class | EF Core loads and changes it in place | — |
| request model without `TenantId` | the client can't pick a tenant | binding the entity directly (see 8.1 over-posting) |
| 404 for another tenant's id | doesn't reveal that the id exists | 403 confirms it does |
| index on `TenantId` | every query filters on it | a full table scan per request as tenants grow |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| reads (query filter) | each tenant sees only its rows | no tenant → nothing; admin `IgnoreQueryFilters` → all; `FromSql` still filtered | other tenant's id: find null, rename false, delete 0; raw `ExecuteSql` bypasses |
| writes (stamping) | new rows get the current tenant | a `TenantId` set in code is overwritten | no tenant, moving a row, editing another tenant's row → throw |
| `TenantContext` | — | same tenant twice is fine | switching tenant mid-request throws |
| HTTP | each tenant's own list; `/me` | body `tenantId` ignored; header case doesn't matter | no header 400, unknown 404, other tenant's id 404 (GET/PUT/DELETE), empty name 400 |
| manual filter | `Where` by hand works | forgotten `Where` leaks everyone | trusting the client's `TenantId` writes into another tenant |
| captured filter | the first tenant sees its own rows | a second tenant sees the first's | — |
| database per tenant | separate rows, no filter needed | new tenant starts empty | blank tenant id throws |
| tenant lookup | host, claim, path | main site, `www`, nested subdomain, no path prefix → null | wrong domain, look-alike domain, not logged in → null |
