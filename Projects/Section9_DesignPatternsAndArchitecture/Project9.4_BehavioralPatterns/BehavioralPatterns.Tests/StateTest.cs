using BehavioralPatterns.Contexts;
using BehavioralPatterns.States;

namespace BehavioralPatterns.Tests;

public class StateTest
{
    [Fact]
    public void Transitions_PositivePositive_HappyPathAndCancelBeforeShipping()
    {
        var delivered = new OrderContext();
        delivered.Pay();
        delivered.Ship();
        delivered.Deliver();

        var cancelled = new OrderContext();
        cancelled.Pay();
        cancelled.Cancel();

        Assert.IsType<DeliveredState>(delivered.State);
        Assert.Equal(["pending", "paid", "shipped", "delivered"], delivered.History);
        Assert.Equal(["pending", "paid", "cancelled"], cancelled.History);
    }

    [Fact]
    public void Transitions_PositiveNegative_ActionNotAllowedInThisState_ThrowsAndStateStays()
    {
        var order = new OrderContext();

        Assert.Throws<InvalidOperationException>(() => order.Ship());    // not paid yet
        order.Pay();
        order.Ship();
        var error = Assert.Throws<InvalidOperationException>(() => order.Cancel());

        Assert.Equal("Can't cancel an order that is shipped.", error.Message);
        Assert.IsType<ShippedState>(order.State);
    }

    [Fact]
    public void Transitions_NegativeNegative_FinishedOrdersAcceptNothing_AndNullStateRejected()
    {
        var order = new OrderContext();
        order.Cancel();

        Assert.Throws<InvalidOperationException>(() => order.Pay());
        Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Throws<ArgumentNullException>(() => order.MoveTo(null!));
        Assert.Equal(["pending", "cancelled"], order.History);
    }
}
