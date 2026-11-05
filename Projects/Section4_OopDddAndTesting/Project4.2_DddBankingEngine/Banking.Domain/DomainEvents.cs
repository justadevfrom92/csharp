namespace Banking.Domain;

// DOMAIN EVENTS: facts about something that already happened, named in the past tense.
public interface IDomainEvent
{
    DateTimeOffset HappenedAt { get; }
}

public record AccountOpened(Guid AccountId, string OwnerName, DateTimeOffset HappenedAt) : IDomainEvent;

public record MoneyDeposited(Guid AccountId, Money Amount, Money NewBalance, DateTimeOffset HappenedAt) : IDomainEvent;

public record MoneyWithdrawn(Guid AccountId, Money Amount, Money NewBalance, DateTimeOffset HappenedAt) : IDomainEvent;

public record AccountClosed(Guid AccountId, DateTimeOffset HappenedAt) : IDomainEvent;
