namespace EfCoreModelling.Models;

// The answer to "how much of each material was sold?". "required" + "init": every value must be set when it's
// created (compile error otherwise), and never changed afterwards.
public record ProductConsumptionModel
{
    public required Guid ProductId { get; init; }

    public required string Name { get; init; }

    public required decimal AmountSold { get; init; }
}
