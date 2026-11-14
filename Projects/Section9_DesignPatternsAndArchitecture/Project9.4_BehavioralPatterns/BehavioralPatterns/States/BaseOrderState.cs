using BehavioralPatterns.Contexts;

namespace BehavioralPatterns.States;

// Default: every action is NOT allowed. Each state overrides only the actions it allows.
// (Abstract base class: shared code, never created on its own.)
public abstract class BaseOrderState : IOrderState
{
    public abstract string Name { get; }

    public virtual void Pay(OrderContext order)
    {
        throw NotAllowed("pay");
    }

    public virtual void Ship(OrderContext order)
    {
        throw NotAllowed("ship");
    }

    public virtual void Deliver(OrderContext order)
    {
        throw NotAllowed("deliver");
    }

    public virtual void Cancel(OrderContext order)
    {
        throw NotAllowed("cancel");
    }

    private InvalidOperationException NotAllowed(string action)
    {
        return new InvalidOperationException($"Can't {action} an order that is {Name}.");
    }
}
