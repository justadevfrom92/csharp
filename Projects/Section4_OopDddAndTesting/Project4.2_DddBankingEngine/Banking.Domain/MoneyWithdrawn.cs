namespace Banking.Domain;

public record MoneyWithdrawn(Guid AccountId, Money Amount, Money NewBalance, DateTimeOffset HappenedAt) : IDomainEvent;
