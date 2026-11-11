namespace IdentityJwtPolicies.Models;

public record TokenResponseModel(string AccessToken, DateTime ExpiresAtUtc);
