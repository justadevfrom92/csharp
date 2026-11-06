namespace Banking.Domain.Events;

public record AccountOpenedEvent(Guid AccountId, string OwnerName, DateTimeOffset HappenedAt) : IDomainEvent;
