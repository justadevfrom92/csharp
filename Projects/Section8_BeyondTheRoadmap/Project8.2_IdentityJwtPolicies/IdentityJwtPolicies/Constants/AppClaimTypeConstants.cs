namespace IdentityJwtPolicies.Constants;

// Our own claim names (besides the standard ones like ClaimTypes.Role). const so attributes/policies can use them.
public static class AppClaimTypeConstants
{
    public const string Department = "department";
    public const string BirthYear = "birth_year";
}
