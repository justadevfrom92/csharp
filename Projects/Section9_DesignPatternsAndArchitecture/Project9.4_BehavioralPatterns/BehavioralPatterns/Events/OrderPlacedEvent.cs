namespace BehavioralPatterns.Events;

public record OrderPlacedEvent(int OrderId, string Customer, decimal Total);
