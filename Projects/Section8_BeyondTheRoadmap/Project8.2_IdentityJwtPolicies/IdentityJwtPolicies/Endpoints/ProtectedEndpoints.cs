using System.Security.Claims;
using IdentityJwtPolicies.Constants;

namespace IdentityJwtPolicies.Endpoints;

public static class ProtectedEndpoints
{
    public static void MapProtectedEndpoints(this IEndpointRouteBuilder app)
    {
        // Any valid token.
        app.MapGet("/me", (ClaimsPrincipal user) => TypedResults.Ok(new
        {
            email = user.Identity!.Name,
            roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value),
            department = user.FindFirst(AppClaimTypeConstants.Department)?.Value,
        }))
        .RequireAuthorization();

        // ROLE-based policy.
        app.MapGet("/admin/dashboard", () => TypedResults.Ok("Admin dashboard"))
            .RequireAuthorization(PolicyNameConstants.AdminOnly);

        // CLAIM-based policy.
        app.MapGet("/reports/finance", () => TypedResults.Ok("Finance report"))
            .RequireAuthorization(PolicyNameConstants.FinanceDepartment);

        // CUSTOM requirement + handler.
        app.MapGet("/content/adults-only", () => TypedResults.Ok("Adults-only content"))
            .RequireAuthorization(PolicyNameConstants.AtLeast18);
    }
}
