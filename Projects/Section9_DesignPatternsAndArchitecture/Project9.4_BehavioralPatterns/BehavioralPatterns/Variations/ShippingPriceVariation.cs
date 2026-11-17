namespace BehavioralPatterns.Variations;

// The shipping prices of the Strategy classes (Strategies/, original), written without classes.
public static class ShippingPriceVariation
{
    // A DICTIONARY OF LAMBDAS: still "pick a strategy by name", but each strategy is one Func, not a class.
    // Good when strategies are small and have no dependencies.
    private static readonly Dictionary<string, Func<decimal, decimal, decimal>> strategies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["standard"] = (total, weight) => 4.99m + weight * 1.00m,
        ["express"] = (total, weight) => 14.99m + weight * 2.50m,
        ["free-over-100"] = (total, weight) => total >= 100m ? 0m : 4.99m + weight * 1.00m,
    };

    public static decimal WithFuncDictionary(string option, decimal total, decimal weight)
    {
        if (!strategies.TryGetValue(option, out Func<decimal, decimal, decimal>? strategy))
        {
            throw new KeyNotFoundException($"Unknown shipping option '{option}'.");
        }
        return strategy(total, weight);
    }

    // A SWITCH: shortest of all, but every new option means EDITING this method (the Strategy pattern avoids that).
    public static decimal WithSwitch(string option, decimal total, decimal weight)
    {
        return option.ToLowerInvariant() switch
        {
            "standard" => 4.99m + weight * 1.00m,
            "express" => 14.99m + weight * 2.50m,
            "free-over-100" => total >= 100m ? 0m : 4.99m + weight * 1.00m,
            _ => throw new KeyNotFoundException($"Unknown shipping option '{option}'."),
        };
    }
}
