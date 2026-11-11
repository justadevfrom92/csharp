using IdentityJwtPolicies.Constants;
using IdentityJwtPolicies.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace IdentityJwtPolicies.Handlers;

// Decides whether the logged-in user meets a MinimumAgeRequirement, using their "birth_year" claim.
public class MinimumAgeAuthorizationHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    private readonly TimeProvider timeProvider;

    public MinimumAgeAuthorizationHandler(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        string? birthYearText = context.User.FindFirst(AppClaimTypeConstants.BirthYear)?.Value;
        if (int.TryParse(birthYearText, out int birthYear))
        {
            int age = timeProvider.GetUtcNow().Year - birthYear; // simple: ignores the birthday within the year
            if (age >= requirement.MinimumAge)
            {
                context.Succeed(requirement);
            }
        }
        // Not calling Succeed = not allowed. (No claim at all also means not allowed.)
        return Task.CompletedTask;
    }
}
