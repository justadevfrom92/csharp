using BehavioralPatterns.Contexts;

namespace BehavioralPatterns.States;

public class ShippedState : BaseOrderState
{
    public override string Name => "shipped";

    public override void Deliver(OrderContext order)
    {
        order.MoveTo(new DeliveredState());
    }
}
