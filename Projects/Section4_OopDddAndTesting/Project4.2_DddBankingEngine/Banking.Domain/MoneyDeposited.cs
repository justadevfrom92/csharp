namespace Banking.Domain;

public record MoneyDeposited(Guid AccountId, Money Amount, Money NewBalance, DateTimeOffset HappenedAt) : IDomainEvent;
