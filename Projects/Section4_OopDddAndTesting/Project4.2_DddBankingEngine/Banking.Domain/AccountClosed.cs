namespace Banking.Domain;

public record AccountClosed(Guid AccountId, DateTimeOffset HappenedAt) : IDomainEvent;
