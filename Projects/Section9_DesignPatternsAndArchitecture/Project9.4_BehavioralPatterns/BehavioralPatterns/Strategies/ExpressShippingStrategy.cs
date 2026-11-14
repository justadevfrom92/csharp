namespace BehavioralPatterns.Strategies;

// 14.99 + 2.50 per kg.
public class ExpressShippingStrategy : IShippingCostStrategy
{
    public string Name => "express";

    public decimal Calculate(decimal orderTotal, decimal weightKilograms)
    {
        return 14.99m + weightKilograms * 2.50m;
    }
}
