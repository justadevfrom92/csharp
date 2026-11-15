using IdentityJwtPolicies.Seeders;
using IdentityJwtPolicies.Settings;
using IdentityJwtPolicies.Tests.Helpers;
using IdentityJwtPolicies.Variations;

namespace IdentityJwtPolicies.Tests;

// Rule styles (IdentityJwtPolicies/Variations/PolicyStyleVariation.cs) and token-validation settings
// (TokenValidationVariation.cs) compared with the original named policies and the app's own validation.
public class VariationsTest : IDisposable
{
    private readonly IdentityAppHelper app = new IdentityAppHelper();
    private readonly JwtSettings settings = new JwtSettings
    {
        Issuer = "https://roadmap.example",
        Audience = "roadmap-api",
        SigningKey = "DEMO-ONLY-signing-key-at-least-32-bytes-long-change-me",
    };

    public void Dispose()
    {
        app.Dispose();
    }

    [Fact]
    public async Task Policies_PositivePositive_InlineAndAssertionStyles_AllowTheRightUsers()
    {
        string admin = await app.GetTokenAsync(IdentityDataSeeder.AdminEmail, IdentityDataSeeder.AdminPassword); // department "it"
        string adult = await app.RegisterAndGetTokenAsync(department: "finance", birthYear: 1990);

        Assert.Equal(200, await app.GetStatusWithTokenAsync("/variations/policies/inline-role", admin));
        Assert.Equal(200, await app.GetStatusWithTokenAsync("/variations/policies/manual", admin));
        Assert.Equal(200, await app.GetStatusWithTokenAsync("/variations/policies/assertion-18", adult));
        Assert.Equal(200, await app.GetStatusWithTokenAsync("/variations/policies/finance-or-it", adult));
        Assert.Equal(200, await app.GetStatusWithTokenAsync("/variations/policies/finance-or-it", admin));  // "it" also allowed
        Assert.Equal(200, await app.GetStatusWithTokenAsync("/content/adults-only", adult));                // the original policy agrees
    }

    [Fact]
    public async Task Policies_PositiveNegative_WrongRoleOrTooYoung_Refused403()
    {
        string teen = await app.RegisterAndGetTokenAsync(department: "sales", birthYear: DateTime.UtcNow.Year - 15);

        Assert.Equal(403, await app.GetStatusWithTokenAsync("/variations/policies/inline-role", teen));
        Assert.Equal(403, await app.GetStatusWithTokenAsync("/variations/policies/manual", teen));          // Forbid() = same 403
        Assert.Equal(403, await app.GetStatusWithTokenAsync("/variations/policies/assertion-18", teen));
        Assert.Equal(403, await app.GetStatusWithTokenAsync("/variations/policies/finance-or-it", teen));    // "sales"
        Assert.Equal(403, await app.GetStatusWithTokenAsync("/content/adults-only", teen));
    }

    [Fact]
    public async Task Tokens_PositiveNegative_ExpiredOneMinuteAgo_DefaultClockSkewStillAcceptsIt()
    {
        string expiredOneMinuteAgo = TokenValidationVariation.CreateToken(settings, minutesAgo: 1);

        Assert.True(await TokenValidationVariation.IsValidAsync(expiredOneMinuteAgo, settings, TimeSpan.FromMinutes(5)));  // library default
        Assert.False(await TokenValidationVariation.IsValidAsync(expiredOneMinuteAgo, settings, TimeSpan.FromSeconds(30))); // the app's setting
        Assert.Equal(401, await app.GetStatusWithTokenAsync("/me", expiredOneMinuteAgo));                                  // the app agrees
        Assert.True(await TokenValidationVariation.IsValidAsync(TokenValidationVariation.CreateToken(settings, 60), settings, TimeSpan.Zero, validateLifetime: false)); // NEVER turn this off
    }

    [Fact]
    public async Task Tokens_NegativeNegative_WrongAudienceOrNoToken_Rejected()
    {
        string otherApi = TokenValidationVariation.CreateToken(settings, minutesAgo: -10, audience: "some-other-api"); // still valid for 10 min

        Assert.False(await TokenValidationVariation.IsValidAsync(otherApi, settings, TimeSpan.Zero));
        Assert.True(await TokenValidationVariation.IsValidAsync(otherApi, settings, TimeSpan.Zero, validateAudience: false)); // a token for ANOTHER api accepted
        Assert.Equal(401, await app.GetStatusWithTokenAsync("/variations/policies/manual", null));
        Assert.Equal(401, await app.GetStatusWithTokenAsync("/variations/policies/inline-role", "not-a-token"));
    }
}
