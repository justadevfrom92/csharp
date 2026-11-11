using Microsoft.AspNetCore.Authorization;

namespace IdentityJwtPolicies.Requirements;

// A CUSTOM policy requirement: just the data ("at least N years old").
// The checking logic lives in Handlers/MinimumAgeAuthorizationHandler.cs.
public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public MinimumAgeRequirement(int minimumAge)
    {
        MinimumAge = minimumAge;
    }

    public int MinimumAge { get; }
}
