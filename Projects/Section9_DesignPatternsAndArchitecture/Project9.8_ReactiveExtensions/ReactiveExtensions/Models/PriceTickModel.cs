namespace ReactiveExtensions.Models;

// One price update for one stock, e.g. ("MSFT", 410.25).
public record PriceTickModel(string Symbol, decimal Price);
