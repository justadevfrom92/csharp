using BehavioralPatterns.Contexts;

namespace BehavioralPatterns.States;

public class PaidState : BaseOrderState
{
    public override string Name => "paid";

    public override void Ship(OrderContext order)
    {
        order.MoveTo(new ShippedState());
    }

    public override void Cancel(OrderContext order)
    {
        order.MoveTo(new CancelledState());
    }
}
