using Banking.Domain.Events;

namespace Banking.Tests.Fakes;

public class FakeEventDispatcher : IDomainEventDispatcher
{
    public List<IDomainEvent> SentEvents { get; } = new();

    public void Dispatch(IDomainEvent domainEvent)
    {
        SentEvents.Add(domainEvent);
    }
}
