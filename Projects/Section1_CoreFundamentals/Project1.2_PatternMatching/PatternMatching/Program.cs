using PatternMatching.Classifiers;
using PatternMatching.Models;
using PatternMatching.Parsers;

Console.WriteLine("=== Relational patterns (credit score) ===");
foreach (int score in new[] { 450, 600, 700, 780, 820 })
{
    Console.WriteLine($"  {score} -> {CreditScoreClassifier.Classify(score)}");
}

Console.WriteLine("=== Property patterns (shipping) ===");
var parcels = new[]
{
    new ParcelModel(0.5m, "US", false),
    new ParcelModel(5m, "US", false),
    new ParcelModel(2m, "US", true),
    new ParcelModel(2m, "DE", true),
    new ParcelModel(2m, "DE", false),
};
foreach (ParcelModel parcel in parcels)
{
    Console.WriteLine($"  {parcel} -> {ShippingCostClassifier.CalculateCost(parcel):0.00}");
}

Console.WriteLine("=== Type patterns + when (payments) ===");
object[] payments = [new CardPaymentModel(50m, "4242"), new CardPaymentModel(2500m, "1111"), new CashPaymentModel(20m)];
foreach (object payment in payments)
{
    Console.WriteLine($"  {PaymentClassifier.Describe(payment)}");
}

Console.WriteLine("=== List patterns (commands) ===");
foreach (string line in new[] { "help", "balance", "deposit 25.50", "withdraw 10" })
{
    Console.WriteLine($"  \"{line}\" -> {BankCommandParser.Parse(line.Split(' '))}");
}
