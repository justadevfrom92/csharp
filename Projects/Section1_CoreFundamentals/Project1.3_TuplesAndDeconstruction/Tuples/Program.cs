using Tuples.Calculators;
using Tuples.Models;

List<decimal> prices = [19.99m, 5.01m, 25.00m];

// 1. Return several values; read them by name or deconstruct them.
(decimal Total, int Count, decimal Average) stats = PriceStatsCalculator.Summarize(prices);
Console.WriteLine($"1. Total {stats.Total}, count {stats.Count}, average {stats.Average}");
var (total, count, _) = PriceStatsCalculator.Summarize(prices); // "_" = don't need the average
Console.WriteLine($"   Deconstructed: {total} over {count} items");

// 2. A nullable tuple for "maybe no answer".
if (PriceStatsCalculator.Range(prices) is (decimal cheapest, decimal dearest))
{
    Console.WriteLine($"2. Range {cheapest} to {dearest}");
}
Console.WriteLine($"   Empty list: {(PriceStatsCalculator.Range([]) is null ? "no range" : "?")}");

// 3. Switch on two values at once.
Console.WriteLine($"3. Shipping US standard {ShippingCalculator.Cost("us", false)}, US express {ShippingCalculator.Cost("US", true)}, " +
    $"FR standard {ShippingCalculator.Cost("FR", false)}, FR express {ShippingCalculator.Cost("FR", true)}");

// 4. Swap without a temporary variable.
int left = 1;
int right = 2;
(left, right) = (right, left);
Console.WriteLine($"4. Swapped: left {left}, right {right}");

// 5. Deconstruct a class (hand-written Deconstruct) and dictionary entries.
var (weight, destination) = new ParcelModel(2.5m, "Paris");
Console.WriteLine($"5. Parcel: {weight} kg to {destination}");
var stock = new Dictionary<string, int> { ["Tea"] = 12, ["Coffee"] = 0 };
foreach (var (product, quantity) in stock)
{
    Console.WriteLine($"   {product}: {quantity}");
}

// 6. Tuples in LINQ: element names are taken from the expressions.
var cheapItems = prices.Index().Where(entry => entry.Item < 20m).Select(entry => (entry.Index, Price: entry.Item));
foreach (var (index, price) in cheapItems)
{
    Console.WriteLine($"6. Item #{index} costs {price}");
}
