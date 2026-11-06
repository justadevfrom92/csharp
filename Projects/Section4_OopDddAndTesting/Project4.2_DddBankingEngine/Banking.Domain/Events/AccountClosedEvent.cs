namespace Banking.Domain.Events;

public record AccountClosedEvent(Guid AccountId, DateTimeOffset HappenedAt) : IDomainEvent;
