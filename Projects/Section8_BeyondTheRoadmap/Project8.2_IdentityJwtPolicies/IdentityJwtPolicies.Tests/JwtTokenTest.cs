using System.Security.Claims;
using System.Text;
using IdentityJwtPolicies.Seeders;
using IdentityJwtPolicies.Tests.Helpers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityJwtPolicies.Tests;

// PositivePositive - a token the app issued -> accepted
// PositiveNegative - a correctly SIGNED token that breaks a rule (expired, wrong audience) -> 401
// NegativeNegative - a malformed, tampered or wrongly-signed token -> 401
public class JwtTokenTest
{
    // The same settings as appsettings.json, to build tokens by hand.
    private const string Issuer = "https://roadmap.example";
    private const string Audience = "roadmap-api";
    private const string SigningKey = "DEMO-ONLY-signing-key-at-least-32-bytes-long-change-me";

    private static string CreateToken(string issuer, string audience, string signingKey, DateTime notBeforeUtc, DateTime expiresUtc)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, "hand-made@example.com")]),
            NotBefore = notBeforeUtc,
            Expires = expiresUtc,
            IssuedAt = notBeforeUtc,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    [Fact]
    public async Task Token_PositivePositive_TokenFromLogin_IsAccepted()
    {
        using var app = new IdentityAppHelper();
        string token = await app.GetTokenAsync(IdentityDataSeeder.AdminEmail, IdentityDataSeeder.AdminPassword);

        Assert.Equal(200, await app.GetStatusWithTokenAsync("/me", token));
    }

    [Fact]
    public async Task Token_PositiveNegative_ExpiredButCorrectlySigned_Returns401()
    {
        using var app = new IdentityAppHelper();
        string expired = CreateToken(Issuer, Audience, SigningKey, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1));

        Assert.Equal(401, await app.GetStatusWithTokenAsync("/me", expired));
    }

    [Fact]
    public async Task Token_PositiveNegative_WrongAudience_Returns401()
    {
        using var app = new IdentityAppHelper();
        string forAnotherApi = CreateToken(Issuer, "some-other-api", SigningKey, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(30));

        Assert.Equal(401, await app.GetStatusWithTokenAsync("/me", forAnotherApi));
    }

    [Fact]
    public async Task Token_NegativeNegative_SignedWithAnotherKey_Returns401()
    {
        using var app = new IdentityAppHelper();
        string forged = CreateToken(Issuer, Audience, "an-attackers-own-signing-key-also-32-bytes-long", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(30));

        Assert.Equal(401, await app.GetStatusWithTokenAsync("/me", forged));
    }

    [Fact]
    public async Task Token_NegativeNegative_TamperedPayloadOrGarbage_Returns401()
    {
        using var app = new IdentityAppHelper();
        string real = await app.GetTokenAsync(IdentityDataSeeder.AdminEmail, IdentityDataSeeder.AdminPassword);
        string[] parts = real.Split('.');
        // Swap the payload for one claiming to be someone else; the signature no longer matches.
        string fakePayload = Base64UrlEncoder.Encode("{\"http://schemas.microsoft.com/ws/2008/06/identity/claims/role\":\"Admin\",\"exp\":9999999999}");
        string tampered = parts[0] + "." + fakePayload + "." + parts[2];

        Assert.Equal(401, await app.GetStatusWithTokenAsync("/me", tampered));
        Assert.Equal(401, await app.GetStatusWithTokenAsync("/me", "not.a.token"));
        Assert.Equal(401, await app.GetStatusWithTokenAsync("/me", null));
    }
}
