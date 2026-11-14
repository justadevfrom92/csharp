using FluentBuilderAndDsl.Builders;
using FluentBuilderAndDsl.Evaluators;
using FluentBuilderAndDsl.Models;
using FluentBuilderAndDsl.Parsers;

// ---------- 1. Fluent builder ----------
OrderModel smallOrder = new OrderBuilder()
    .ForCustomer("Ann")
    .AddLine("MS-01", 1, 12.50m)
    .Build();

OrderModel bigOrder = new OrderBuilder()
    .ForCustomer("Bob")
    .ShipTo("ca")
    .AddLine("KB-01", 2, 25m)
    .AddLine("MN-01", 1, 180m)
    .AddLine("CB-01", 3, 4m)
    .WithNote("Leave at the door")
    .Build();

Console.WriteLine($"Built: {bigOrder.Customer}, {bigOrder.Country}, {bigOrder.Lines.Count} lines, {bigOrder.ItemCount} items, subtotal {bigOrder.Subtotal:0.00}");

try
{
    new OrderBuilder().ForCustomer("Cid").Build();
}
catch (InvalidOperationException exception)
{
    Console.WriteLine($"Build() without lines -> {exception.Message}");
}
Console.WriteLine();

// ---------- 2. External DSL: rules from a text file ----------
string rulesText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "pricing-rules.txt"));
List<PricingRuleModel> rules = PricingRuleParser.Parse(rulesText);
Console.WriteLine($"Parsed {rules.Count} rules from pricing-rules.txt");

foreach (OrderModel order in new[] { smallOrder, bigOrder })
{
    PriceQuoteModel quote = PricingRuleEvaluator.Quote(order, rules);
    Console.WriteLine($"{order.Customer}: subtotal {quote.Subtotal:0.00} - {quote.DiscountPercent}% ({quote.Discount:0.00}) + shipping {quote.Shipping:0.00} = {quote.Total:0.00}");
    foreach (string applied in quote.AppliedRules)
    {
        Console.WriteLine($"    {applied}");
    }
}

try
{
    PricingRuleParser.Parse("WHEN total > 100 THEN discount 10%\nWHEN colour = red THEN discount 5%");
}
catch (FormatException exception)
{
    Console.WriteLine($"Bad rule file -> {exception.Message}");
}
