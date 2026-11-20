namespace AsyncStreams.Models;

// One price update. A record: made once, sent, never changed.
public record PriceTickModel(int Number, string Symbol, decimal Price);
