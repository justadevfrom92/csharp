namespace BehavioralPatterns.Strategies;

// Free from 100.00, otherwise standard.
public class FreeOverThresholdShippingStrategy : IShippingCostStrategy
{
    public const decimal Threshold = 100m;
    private readonly StandardShippingStrategy fallback = new StandardShippingStrategy();

    public string Name => "free-over-100";

    public decimal Calculate(decimal orderTotal, decimal weightKilograms)
    {
        return orderTotal >= Threshold ? 0m : fallback.Calculate(orderTotal, weightKilograms);
    }
}
