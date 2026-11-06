using Banking.Domain.Events;

namespace Banking.Domain.Dispatchers;

// Whoever wants to react to events (send email, write audit log...) implements this.
public interface IDomainEventDispatcher
{
    void Dispatch(IDomainEvent domainEvent);
}
