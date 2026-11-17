namespace BehavioralPatterns.Variations;

// The order life cycle as an ENUM + a table of allowed moves, instead of one class per state (States/, original).
// Everything is visible in one place; the class version is better when each state has real BEHAVIOUR of its own.
public enum OrderStatusVariation
{
    Pending,
    Paid,
    Shipped,
    Delivered,
    Cancelled,
}
