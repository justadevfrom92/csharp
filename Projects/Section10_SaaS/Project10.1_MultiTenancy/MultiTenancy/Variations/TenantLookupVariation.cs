using System.Security.Claims;

namespace MultiTenancy.Variations;

// Other places to read the tenant from, instead of the X-Tenant header (original).
public static class TenantLookupVariation
{
    // SUBDOMAIN: "acme.myapp.com" -> "acme". Customers get their own address; needs wildcard DNS + certificate.
    public static string? FromHost(string host, string appDomain)
    {
        string hostOnly = host.Split(':')[0].ToLowerInvariant();
        string suffix = "." + appDomain.ToLowerInvariant();
        if (!hostOnly.EndsWith(suffix))
        {
            return null;
        }
        string tenant = hostOnly[..^suffix.Length];
        return tenant.Length == 0 || tenant.Contains('.') || tenant == "www" ? null : tenant;
    }

    // CLAIM in the signed login token (8.2 JWT / 7.1 cookie): the client can't change it without breaking
    // the signature - the safest source.
    public static string? FromClaims(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true ? user.FindFirst("tenant")?.Value : null;
    }

    // URL PATH: "/t/acme/projects" -> "acme". Easy to test and share links; every route needs the prefix.
    public static string? FromPath(string path)
    {
        string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && parts[0] == "t" ? parts[1].ToLowerInvariant() : null;
    }
}
