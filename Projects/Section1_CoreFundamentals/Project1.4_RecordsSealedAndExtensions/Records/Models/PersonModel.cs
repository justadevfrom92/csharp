namespace Records.Models;

// For the "with" examples: p2 = p1 with { Name = "..." } makes a COPY with some properties changed.
// Name is checked in its "init" accessor (.NET 10 "field" = the hidden backing field). The constructor assigns
// THROUGH the property, so the check runs on "new" AND on "with".
// Found by the tests: writing it as a positional record with "{ init => check } = Name" skipped the check in the
// constructor (the "= Name" initializer writes the field directly) - see Variations/UncheckedPersonVariation.
public record PersonModel
{
    public PersonModel(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public string Name { get; init => field = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Name is required.", nameof(Name)) : value; }

    public int Age { get; init; }

    // A LIST inside a record: "with" copies the REFERENCE, so both copies share the same list (a shallow copy).
    // It also breaks value equality: two people with the same name and age have DIFFERENT lists, so they aren't equal.
    public List<string> Orders { get; init; } = [];
}
