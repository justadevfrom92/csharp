namespace Banking.Domain;

public record AccountOpened(Guid AccountId, string OwnerName, DateTimeOffset HappenedAt) : IDomainEvent;
