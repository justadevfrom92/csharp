namespace ServiceRegistration.Models;

public record JobRunModel(string JobName, DateTimeOffset RanAt, bool Succeeded, string? Error);
