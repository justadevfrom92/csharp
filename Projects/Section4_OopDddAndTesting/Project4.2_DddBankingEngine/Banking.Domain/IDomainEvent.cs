namespace Banking.Domain;

// DOMAIN EVENTS: facts about something that already happened, named in the past tense.
public interface IDomainEvent
{
    DateTimeOffset HappenedAt { get; }
}
