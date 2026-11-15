namespace TransactionalCheckout.Variations;

// What the database looks like after a checkout attempt.
public record CheckoutOutcomeVariation(int Orders, decimal Wallet, int MouseStock, string? Error);
