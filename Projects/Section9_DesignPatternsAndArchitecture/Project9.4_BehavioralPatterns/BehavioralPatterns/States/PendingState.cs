using BehavioralPatterns.Contexts;

namespace BehavioralPatterns.States;

public class PendingState : BaseOrderState
{
    public override string Name => "pending";

    public override void Pay(OrderContext order)
    {
        order.MoveTo(new PaidState());
    }

    public override void Cancel(OrderContext order)
    {
        order.MoveTo(new CancelledState());
    }
}
