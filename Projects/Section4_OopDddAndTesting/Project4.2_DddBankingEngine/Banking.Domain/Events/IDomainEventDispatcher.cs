namespace Banking.Domain.Events;

// Whoever wants to react to events (send email, write audit log...) implements this.
public interface IDomainEventDispatcher
{
    void Dispatch(IDomainEvent domainEvent);
}
