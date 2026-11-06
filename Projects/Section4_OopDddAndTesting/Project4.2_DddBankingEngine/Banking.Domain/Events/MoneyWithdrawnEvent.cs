using Banking.Domain.ValueObjects;

namespace Banking.Domain.Events;

public record MoneyWithdrawnEvent(Guid AccountId, MoneyValueObject Amount, MoneyValueObject NewBalance, DateTimeOffset HappenedAt) : IDomainEvent;
