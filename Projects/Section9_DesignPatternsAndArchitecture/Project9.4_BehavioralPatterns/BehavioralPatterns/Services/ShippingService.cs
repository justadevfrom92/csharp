using BehavioralPatterns.Strategies;

namespace BehavioralPatterns.Services;

// Picks a strategy BY NAME at runtime (e.g. what the customer chose at checkout).
// Adding a new shipping option = one new class. This service never changes: no growing if/else or switch.
public class ShippingService
{
    private readonly Dictionary<string, IShippingCostStrategy> strategies;

    public ShippingService(IEnumerable<IShippingCostStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        this.strategies = strategies.ToDictionary(strategy => strategy.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> OptionNames => strategies.Keys;

    public decimal Quote(string option, decimal orderTotal, decimal weightKilograms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(option);
        if (orderTotal < 0 || weightKilograms <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weightKilograms), "Total can't be negative and weight must be more than 0.");
        }
        if (!strategies.TryGetValue(option, out IShippingCostStrategy? strategy))
        {
            throw new KeyNotFoundException($"Unknown shipping option '{option}'. Options: {string.Join(", ", strategies.Keys)}.");
        }
        return strategy.Calculate(orderTotal, weightKilograms);
    }
}
