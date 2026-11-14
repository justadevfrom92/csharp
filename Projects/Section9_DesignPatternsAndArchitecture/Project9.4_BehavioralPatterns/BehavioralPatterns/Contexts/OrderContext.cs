using BehavioralPatterns.States;

namespace BehavioralPatterns.Contexts;

// The object whose behaviour depends on its state (the pattern calls it the "context").
// It just passes every action to its CURRENT state object.
//
//   pending --Pay--> paid --Ship--> shipped --Deliver--> delivered
//      |              |
//      +---Cancel-----+--> cancelled
public class OrderContext
{
    private readonly List<string> history = new();

    public OrderContext()
    {
        State = new PendingState();
        history.Add(State.Name);
    }

    public IOrderState State { get; private set; }

    public IReadOnlyList<string> History => history;

    public void Pay()
    {
        State.Pay(this);
    }

    public void Ship()
    {
        State.Ship(this);
    }

    public void Deliver()
    {
        State.Deliver(this);
    }

    public void Cancel()
    {
        State.Cancel(this);
    }

    // Only the states call this.
    public void MoveTo(IOrderState nextState)
    {
        ArgumentNullException.ThrowIfNull(nextState);
        State = nextState;
        history.Add(nextState.Name);
    }
}
