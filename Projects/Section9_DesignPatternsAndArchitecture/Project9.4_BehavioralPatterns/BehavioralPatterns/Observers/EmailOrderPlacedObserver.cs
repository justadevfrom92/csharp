using BehavioralPatterns.Events;

namespace BehavioralPatterns.Observers;

public class EmailOrderPlacedObserver : IOrderPlacedObserver
{
    public List<string> SentEmails { get; } = new();

    public void OnOrderPlaced(OrderPlacedEvent placedEvent)
    {
        SentEmails.Add($"To {placedEvent.Customer}: order {placedEvent.OrderId} confirmed ({placedEvent.Total:0.00}).");
    }
}
