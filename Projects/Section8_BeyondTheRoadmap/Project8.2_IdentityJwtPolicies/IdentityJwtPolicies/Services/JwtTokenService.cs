using System.Security.Claims;
using System.Text;
using IdentityJwtPolicies.Models;
using IdentityJwtPolicies.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IdentityJwtPolicies.Services;

// Creates the JWT a client sends back as "Authorization: Bearer <token>".
// A JWT is three base64 parts: header.payload.signature. The payload (the claims) is READABLE by anyone -
// never put secrets in it. The signature proves WE made it and nobody changed it.
public class JwtTokenService
{
    private readonly JwtSettings settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        this.settings = settings.Value;
    }

    public TokenResponseModel CreateToken(IEnumerable<Claim> claims)
    {
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,       // who made the token
            Audience = settings.Audience,   // who it's meant for
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        };
        string token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new TokenResponseModel(token, expiresAtUtc);
    }
}
