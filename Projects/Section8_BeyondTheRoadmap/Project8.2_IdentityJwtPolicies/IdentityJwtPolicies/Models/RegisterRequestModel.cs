using System.ComponentModel.DataAnnotations;

namespace IdentityJwtPolicies.Models;

public record RegisterRequestModel(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password,
    [property: Required, StringLength(50)] string Department,
    [property: Range(1900, 2100)] int BirthYear);
