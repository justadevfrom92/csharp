using BehavioralPatterns.Contexts;

namespace BehavioralPatterns.States;

// STATE: each state of an object is its own class, and decides what's allowed IN that state.
// Instead of "if (status == Paid && ...)" scattered everywhere, PaidState.Ship() simply knows the answer.
public interface IOrderState
{
    string Name { get; }

    void Pay(OrderContext order);

    void Ship(OrderContext order);

    void Deliver(OrderContext order);

    void Cancel(OrderContext order);
}
