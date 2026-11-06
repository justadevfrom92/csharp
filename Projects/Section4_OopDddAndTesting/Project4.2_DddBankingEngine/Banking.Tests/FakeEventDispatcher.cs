using Banking.Domain;

namespace Banking.Tests;

public class FakeEventDispatcher : IDomainEventDispatcher
{
    public List<IDomainEvent> SentEvents { get; } = new();

    public void Dispatch(IDomainEvent domainEvent)
    {
        SentEvents.Add(domainEvent);
    }
}
