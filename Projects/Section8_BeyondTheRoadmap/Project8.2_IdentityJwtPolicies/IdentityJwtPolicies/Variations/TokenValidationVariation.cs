using System.Text;
using IdentityJwtPolicies.Settings;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityJwtPolicies.Variations;

// Validating a token with different settings, to see what each check does.
// The app (Program.cs, original) checks issuer, audience, signature and lifetime with ClockSkew = 30 seconds.
public static class TokenValidationVariation
{
    public static async Task<bool> IsValidAsync(string token, JwtSettings settings, TimeSpan clockSkew, bool validateLifetime = true, bool validateAudience = true)
    {
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            ValidateAudience = validateAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            ValidateLifetime = validateLifetime,
            ClockSkew = clockSkew, // the library's DEFAULT is 5 minutes: an expired token still works for 5 more minutes
        };
        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);
        return result.IsValid;
    }

    // Makes a token that expired "minutesAgo" minutes ago (for the tests).
    public static string CreateToken(JwtSettings settings, int minutesAgo, string? audience = null)
    {
        DateTime expires = DateTime.UtcNow.AddMinutes(-minutesAgo);
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = audience ?? settings.Audience,
            NotBefore = expires.AddMinutes(-30),
            IssuedAt = expires.AddMinutes(-30),
            Expires = expires,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256),
        });
    }
}
