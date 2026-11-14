using BehavioralPatterns.Events;

namespace BehavioralPatterns.Observers;

// 1 point per whole currency unit spent.
public class LoyaltyPointsOrderPlacedObserver : IOrderPlacedObserver
{
    public Dictionary<string, int> PointsByCustomer { get; } = new();

    public void OnOrderPlaced(OrderPlacedEvent placedEvent)
    {
        PointsByCustomer[placedEvent.Customer] = PointsByCustomer.GetValueOrDefault(placedEvent.Customer) + (int)placedEvent.Total;
    }
}
