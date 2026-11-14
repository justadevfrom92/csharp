namespace BehavioralPatterns.Strategies;

// 4.99 + 1.00 per kg.
public class StandardShippingStrategy : IShippingCostStrategy
{
    public string Name => "standard";

    public decimal Calculate(decimal orderTotal, decimal weightKilograms)
    {
        return 4.99m + weightKilograms * 1.00m;
    }
}
