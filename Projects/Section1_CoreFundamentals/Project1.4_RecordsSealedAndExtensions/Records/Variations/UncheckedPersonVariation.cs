namespace Records.Variations;

// The check written the usual way: in a property INITIALIZER, which only the CONSTRUCTOR runs.
// "with" copies the object and then calls the init accessor, so it never sees the check: valid with { Name = "" }
// succeeds. PersonModel (original) checks inside "init" and assigns through the property in its constructor.
public record UncheckedPersonVariation(string Name)
{
    public string Name { get; init; } = string.IsNullOrWhiteSpace(Name) ? throw new ArgumentException("Name is required.", nameof(Name)) : Name;
}
