using System.Security.Claims;
using IdentityJwtPolicies.Constants;

namespace IdentityJwtPolicies.Variations;

// The same kinds of rules as the named policies in Program.cs (original), written other ways.
// Mapped under /variations/policies/... (see Program.cs).
public static class PolicyStyleVariation
{
    public static void MapPolicyVariations(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/variations/policies");

        // INLINE policy: built right on the endpoint, no name. Same as the AdminOnly policy, but can't be reused.
        group.MapGet("/inline-role", () => TypedResults.Ok("admin"))
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // RequireAssertion: a custom rule as a LAMBDA instead of a requirement + handler class (original AtLeast18).
        // Shorter, but it can't get services from DI (the original handler uses TimeProvider, so tests can fake the date).
        group.MapGet("/assertion-18", () => TypedResults.Ok("adult"))
            .RequireAuthorization(policy => policy.RequireAssertion(context =>
                int.TryParse(context.User.FindFirst(AppClaimTypeConstants.BirthYear)?.Value, out int birthYear)
                && DateTime.UtcNow.Year - birthYear >= 18));

        // RequireClaim with SEVERAL allowed values: department is "finance" OR "it".
        group.MapGet("/finance-or-it", () => TypedResults.Ok("finance or it"))
            .RequireAuthorization(policy => policy.RequireClaim(AppClaimTypeConstants.Department, "finance", "it"));

        // Checked INSIDE the endpoint: any logged-in user gets in, then the code decides. Invisible to tooling
        // (OpenAPI, policy lists) and easy to forget on the next endpoint.
        group.MapGet("/manual", (ClaimsPrincipal user) => user.IsInRole("Admin") ? Results.Ok("admin") : Results.Forbid())
            .RequireAuthorization();
    }
}
