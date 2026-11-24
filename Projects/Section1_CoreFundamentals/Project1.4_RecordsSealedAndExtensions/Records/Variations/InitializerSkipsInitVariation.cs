namespace Records.Variations;

// The trap found by the tests: the check is in "init", but the positional parameter is copied with "= Name".
// That initializer writes the hidden field DIRECTLY, so the constructor skips the check - only "with" runs it.
public record InitializerSkipsInitVariation(string Name)
{
    public string Name { get; init => field = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Name is required.", nameof(Name)) : value; } = Name;
}
