using FluentBuilderAndDsl.Builders;
using FluentBuilderAndDsl.Evaluators;
using FluentBuilderAndDsl.Models;
using FluentBuilderAndDsl.Parsers;

namespace FluentBuilderAndDsl.Tests;

public class PricingDslTest
{
    private const string Rules = """
        # comment
        WHEN total > 100 THEN discount 10%

        when ITEMS >= 5 then DISCOUNT 5%
        WHEN country = CA THEN shipping 4.99
        """;

    private static OrderModel Order(string country, params (int Quantity, decimal Price)[] lines)
    {
        var builder = new OrderBuilder().ForCustomer("Test").ShipTo(country);
        foreach ((int quantity, decimal price) in lines)
        {
            builder.AddLine("SKU", quantity, price);
        }
        return builder.Build();
    }

    // ---------- Parse ----------

    [Fact]
    public void Parse_PositivePositive_ValidRules_SkipsCommentsAndBlankLinesAndIgnoresCase()
    {
        List<PricingRuleModel> rules = PricingRuleParser.Parse(Rules);

        Assert.Equal(3, rules.Count);
        Assert.Equal(new PricingRuleModel(2, "total", ">", "100", "discount", 10m), rules[0]);
        Assert.Equal(new PricingRuleModel(4, "items", ">=", "5", "discount", 5m), rules[1]);
        Assert.Equal("CA", rules[2].Value);
    }

    [Theory]
    [InlineData("WHEN colour = red THEN discount 5%", "Unknown field")]
    [InlineData("WHEN country > CA THEN shipping 1", "only be compared with '='")]
    [InlineData("WHEN total > 100 THEN discount 60%", "1% to 50%")]
    [InlineData("WHEN total > 100 THEN discount 10", "1% to 50%")]
    [InlineData("WHEN total > 100 THEN shipping 5%", "not a percentage")]
    [InlineData("WHEN total > 100 THEN refund 5", "Unknown action")]
    public void Parse_PositiveNegative_RightShapeButBreaksARule_SaysWhichLineAndWhy(string line, string expectedMessage)
    {
        var error = Assert.Throws<FormatException>(() => PricingRuleParser.Parse("# ok\n" + line));

        Assert.StartsWith("Line 2:", error.Message);
        Assert.Contains(expectedMessage, error.Message);
    }

    [Theory]
    [InlineData("discount everything")]
    [InlineData("WHEN total > THEN discount 5%")]
    [InlineData("WHEN total > ten THEN discount 5%")]
    public void Parse_NegativeNegative_Gibberish_ThrowsFormatException(string line)
    {
        Assert.Throws<FormatException>(() => PricingRuleParser.Parse(line));
        Assert.Throws<ArgumentNullException>(() => PricingRuleParser.Parse(null!));
    }

    // ---------- Quote ----------

    [Fact]
    public void Quote_PositivePositive_MatchingRules_AddUpDiscountsAndSetShipping()
    {
        OrderModel order = Order("CA", (5, 30m)); // 150.00, 5 items, Canada

        PriceQuoteModel quote = PricingRuleEvaluator.Quote(order, PricingRuleParser.Parse(Rules));

        Assert.Equal(15, quote.DiscountPercent);  // 10 + 5
        Assert.Equal(22.5m, quote.Discount);
        Assert.Equal(4.99m, quote.Shipping);
        Assert.Equal(132.49m, quote.Total);        // 150 - 22.50 + 4.99
        Assert.Equal(3, quote.AppliedRules.Count);
    }

    [Fact]
    public void Quote_PositiveNegative_NoRuleMatches_FullPriceAndDefaultShipping_AndDiscountIsCapped()
    {
        PriceQuoteModel noMatch = PricingRuleEvaluator.Quote(Order("US", (1, 10m)), PricingRuleParser.Parse(Rules));
        List<PricingRuleModel> greedy = PricingRuleParser.Parse("WHEN total > 0 THEN discount 40%\nWHEN total > 0 THEN discount 40%");
        PriceQuoteModel capped = PricingRuleEvaluator.Quote(Order("US", (1, 100m)), greedy);

        Assert.Equal(0, noMatch.DiscountPercent);
        Assert.Equal(PricingRuleEvaluator.DefaultShipping, noMatch.Shipping);
        Assert.Empty(noMatch.AppliedRules);
        Assert.Equal(50, capped.DiscountPercent); // 80% asked, capped at 50%
    }

    [Fact]
    public void Quote_NegativeNegative_NullOrderOrRules_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PricingRuleEvaluator.Quote(null!, []));
        Assert.Throws<ArgumentNullException>(() => PricingRuleEvaluator.Quote(Order("US", (1, 1m)), null!));
    }
}
