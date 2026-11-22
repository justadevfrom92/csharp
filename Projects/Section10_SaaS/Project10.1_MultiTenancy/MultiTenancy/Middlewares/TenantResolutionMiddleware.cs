using MultiTenancy.Contexts;
using MultiTenancy.Models;
using MultiTenancy.Stores;

namespace MultiTenancy.Middlewares;

// Runs before every endpoint: reads the tenant from the "X-Tenant" header and stores it in TenantContext.
// No header -> 400, unknown tenant -> 404, and the endpoint never runs.
// SECURITY: a header is chosen by the client. A real SaaS takes the tenant from the SIGNED login token
// (a "tenant" claim, see 8.2) or checks the user belongs to it - otherwise anyone can type "globex".
public class TenantResolutionMiddleware
{
    public const string HeaderName = "X-Tenant";

    private readonly RequestDelegate next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    // Scoped services (TenantContext) are injected here, not in the constructor: the middleware itself lives forever.
    public async Task InvokeAsync(HttpContext context, TenantStore store, TenantContext tenantContext)
    {
        string? tenantId = context.Request.Headers[HeaderName];
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            await Results.Problem($"Missing {HeaderName} header.", statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
            return;
        }

        TenantModel? tenant = store.FindById(tenantId);
        if (tenant == null)
        {
            await Results.Problem($"Unknown tenant '{tenantId}'.", statusCode: StatusCodes.Status404NotFound).ExecuteAsync(context);
            return;
        }

        tenantContext.Set(tenant);
        await next(context);
    }
}
