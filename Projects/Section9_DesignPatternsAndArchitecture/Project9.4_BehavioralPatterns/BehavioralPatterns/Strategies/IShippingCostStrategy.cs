namespace BehavioralPatterns.Strategies;

// STRATEGY: several interchangeable ways to do ONE job, each in its own class, all behind one interface.
// The code that USES a strategy doesn't know or care which one it got.
public interface IShippingCostStrategy
{
    string Name { get; }

    decimal Calculate(decimal orderTotal, decimal weightKilograms);
}
