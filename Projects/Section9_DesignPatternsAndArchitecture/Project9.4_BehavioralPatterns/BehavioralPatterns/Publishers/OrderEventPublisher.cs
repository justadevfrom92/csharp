using BehavioralPatterns.Events;
using BehavioralPatterns.Observers;

namespace BehavioralPatterns.Publishers;

// The "subject" of the observer pattern. Subscribe returns an IDisposable: disposing it unsubscribes,
// so an observer can't be forgotten in the list (a classic memory leak with events).
public class OrderEventPublisher
{
    private readonly List<IOrderPlacedObserver> observers = new();

    public IDisposable Subscribe(IOrderPlacedObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (!observers.Contains(observer))
        {
            observers.Add(observer);
        }
        return new Unsubscriber(observers, observer);
    }

    public int ObserverCount => observers.Count;

    // One failing observer must not stop the others: collect the failures, report them at the end.
    public void Publish(OrderPlacedEvent placedEvent)
    {
        ArgumentNullException.ThrowIfNull(placedEvent);
        var failures = new List<Exception>();
        foreach (IOrderPlacedObserver observer in observers.ToList()) // copy: an observer may unsubscribe while we loop
        {
            try
            {
                observer.OnOrderPlaced(placedEvent);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
        if (failures.Count > 0)
        {
            throw new AggregateException("Some observers failed.", failures);
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<IOrderPlacedObserver> observers;
        private readonly IOrderPlacedObserver observer;

        public Unsubscriber(List<IOrderPlacedObserver> observers, IOrderPlacedObserver observer)
        {
            this.observers = observers;
            this.observer = observer;
        }

        public void Dispose()
        {
            observers.Remove(observer);
        }
    }
}
