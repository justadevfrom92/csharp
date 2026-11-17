namespace BehavioralPatterns.Variations;

// The allowed moves between OrderStatusVariation values, as one table.
public static class OrderStatusTransitionVariation
{
    private static readonly Dictionary<(OrderStatusVariation From, string Action), OrderStatusVariation> allowed = new()
    {
        [(OrderStatusVariation.Pending, "pay")] = OrderStatusVariation.Paid,
        [(OrderStatusVariation.Pending, "cancel")] = OrderStatusVariation.Cancelled,
        [(OrderStatusVariation.Paid, "ship")] = OrderStatusVariation.Shipped,
        [(OrderStatusVariation.Paid, "cancel")] = OrderStatusVariation.Cancelled,
        [(OrderStatusVariation.Shipped, "deliver")] = OrderStatusVariation.Delivered,
    };

    public static OrderStatusVariation Apply(OrderStatusVariation current, string action)
    {
        if (!allowed.TryGetValue((current, action), out OrderStatusVariation next))
        {
            throw new InvalidOperationException($"Can't {action} an order that is {current.ToString().ToLowerInvariant()}.");
        }
        return next;
    }
}
