namespace BehavioralPatterns.Results;

public record PlaceOrderResult(bool Succeeded, int OrderId, decimal Shipping, decimal GrandTotal, string? Error);
