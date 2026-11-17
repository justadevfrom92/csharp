using FluentBuilderAndDsl.Models;

namespace FluentBuilderAndDsl.Variations;

// No builder at all: an object initializer with "required" (C# 11) and "init" properties.
//   var order = new OrderInitVariation { Customer = "Ann", Lines = [...] };
// The COMPILER refuses to build it without Customer and Lines (error CS9035) - checked before the program runs,
// which a builder can't do. But only "is it set?": a rule like "at least one line" isn't checked (an empty list compiles).
public class OrderInitVariation
{
    public required string Customer { get; init; }

    public required IReadOnlyList<OrderLineModel> Lines { get; init; }

    public string Country { get; init; } = "US";

    public string? Note { get; init; }
}
