using System.ComponentModel.DataAnnotations;

namespace IdentityJwtPolicies.Models;

public record LoginRequestModel(
    [property: Required] string Email,
    [property: Required] string Password);
