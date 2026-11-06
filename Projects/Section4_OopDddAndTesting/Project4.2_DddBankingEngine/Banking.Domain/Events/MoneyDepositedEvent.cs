using Banking.Domain.ValueObjects;

namespace Banking.Domain.Events;

public record MoneyDepositedEvent(Guid AccountId, MoneyValueObject Amount, MoneyValueObject NewBalance, DateTimeOffset HappenedAt) : IDomainEvent;
