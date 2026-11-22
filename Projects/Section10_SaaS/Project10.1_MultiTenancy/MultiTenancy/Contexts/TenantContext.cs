using MultiTenancy.Models;

namespace MultiTenancy.Contexts;

// "Who is this request for?" Registered as SCOPED: one per request, so two requests for two tenants
// never share it. Filled in by TenantResolutionMiddleware, read by AppDbContext.
public class TenantContext
{
    public TenantModel? Tenant { get; private set; }

    // "" (no tenant) matches no rows, so code that forgot to set a tenant sees NOTHING rather than everything.
    public string TenantId
    {
        get
        {
            return Tenant?.Id ?? "";
        }
    }

    public void Set(TenantModel tenant)
    {
        if (Tenant != null && Tenant.Id != tenant.Id)
        {
            throw new InvalidOperationException("The tenant can't change halfway through a request.");
        }
        Tenant = tenant;
    }
}
