using System.ComponentModel.DataAnnotations;

namespace MultiTenancy.Models;

// What the client sends. No TenantId field: the client can't choose which tenant the data goes to.
public record SaveProjectRequestModel([property: Required, StringLength(100, MinimumLength = 1)] string Name);
