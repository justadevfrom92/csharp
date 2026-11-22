# Cheat Sheet — Section 10: SaaS

Projects: [10.1](../Section10_SaaS/Project10.1_MultiTenancy)

## Multi-tenancy: shared database + `TenantId`

```csharp
// 1. one tenant per request
builder.Services.AddScoped<TenantContext>();
app.UseMiddleware<TenantResolutionMiddleware>(); // reads header / claim / subdomain, fills TenantContext

// 2. every query filtered automatically (read a DbContext PROPERTY, never a captured local)
modelBuilder.Entity<ProjectModel>().HasQueryFilter(project => project.TenantId == CurrentTenantId);

// 3. writes stamped in SaveChanges
if (entry.State == EntityState.Added)
{
    entry.Entity.TenantId = CurrentTenantId;
}

// admin only
database.Projects.IgnoreQueryFilters();
```

| Rule | Why |
|---|---|
| take the tenant from the **signed token claim** in production | a header or subdomain is chosen by the client |
| no tenant → **no rows** | fail closed |
| another tenant's id → **404** | 403 reveals it exists |
| `ExecuteSql("UPDATE ...")` is **not** filtered | add `WHERE TenantId = {tenant}` yourself |
| `FromSql` and `ExecuteDelete` **are** filtered (tested) | EF composes the filter onto them |
| index `TenantId` | every query uses it |
| test that tenant A can't read, change or delete B's data | the bug that matters most |
