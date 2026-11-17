using BehavioralPatterns.Events;

namespace BehavioralPatterns.Variations;

// The observer pattern with a C# "event" instead of OrderEventPublisher + IOrderPlacedObserver (original).
// Shorter - but a plain event calls handlers ONE AFTER ANOTHER and stops at the first exception:
// later handlers are never called. The original catches each failure and still tells everyone.
public class OrderPlacedEventVariation
{
    public event Action<OrderPlacedEvent>? OrderPlaced;

    public void Publish(OrderPlacedEvent placedEvent)
    {
        OrderPlaced?.Invoke(placedEvent);
    }
}
