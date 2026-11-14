namespace ReactiveExtensions.Models;

public record PriceJumpModel(string Symbol, decimal From, decimal To, decimal ChangePercent);
