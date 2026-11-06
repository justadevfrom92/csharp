using Banking.Domain.ValueObjects;

namespace Banking.Domain.Events;

public record MoneyDepositedEvent(Guid AccountId, Money Amount, Money NewBalance, DateTimeOffset HappenedAt) : IDomainEvent;
