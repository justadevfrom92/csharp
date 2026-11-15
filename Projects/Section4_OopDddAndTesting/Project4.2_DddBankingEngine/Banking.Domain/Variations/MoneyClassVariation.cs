namespace Banking.Domain.Variations;

// Money as a plain CLASS. Same checks, but == compares REFERENCES (is it the same object?), not values:
//   new MoneyClassVariation(5, "USD") == new MoneyClassVariation(5, "USD")   ->  false
// To fix that you'd override Equals, GetHashCode, == and != by hand - which "record" writes for you.
public class MoneyClassVariation
{
    public MoneyClassVariation(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Money cannot be negative.", nameof(amount));
        }
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }
}
