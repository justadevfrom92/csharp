using BehavioralPatterns.Events;

namespace BehavioralPatterns.Observers;

// OBSERVER: objects that want to be TOLD when something happens. The publisher keeps a list of them
// and calls each one; it doesn't know what they do (email, stock, loyalty points...).
public interface IOrderPlacedObserver
{
    void OnOrderPlaced(OrderPlacedEvent placedEvent);
}
