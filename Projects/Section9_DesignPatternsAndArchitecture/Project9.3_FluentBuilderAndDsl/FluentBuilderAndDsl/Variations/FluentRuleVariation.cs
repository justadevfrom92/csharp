using FluentBuilderAndDsl.Models;

namespace FluentBuilderAndDsl.Variations;

// The pricing rules as an INTERNAL DSL: plain C# that reads like the text rules.
//   FluentRuleVariation.When(order => order.Subtotal > 100).Discount(10)
// vs the EXTERNAL DSL (original): "WHEN total > 100 THEN discount 10%" in a text file, parsed at runtime.
// Internal: the compiler checks it, any C# condition is possible - but changing a rule means changing code and redeploying.
public class FluentRuleVariation
{
    private readonly Func<OrderModel, bool> condition;

    private FluentRuleVariation(Func<OrderModel, bool> condition)
    {
        this.condition = condition;
    }

    public int DiscountPercent { get; private set; }

    public decimal? Shipping { get; private set; }

    public static FluentRuleVariation When(Func<OrderModel, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return new FluentRuleVariation(condition);
    }

    public FluentRuleVariation Discount(int percent)
    {
        DiscountPercent = percent;
        return this;
    }

    public FluentRuleVariation ShipFor(decimal amount)
    {
        Shipping = amount;
        return this;
    }

    // Same rules as PricingRuleEvaluator: discounts add up (max 50%), the last matching shipping wins (default 9.99).
    public static PriceQuoteModel Quote(OrderModel order, IEnumerable<FluentRuleVariation> rules)
    {
        int percent = 0;
        decimal shipping = 9.99m;
        var applied = new List<string>();
        foreach (FluentRuleVariation rule in rules.Where(rule => rule.condition(order)))
        {
            percent += rule.DiscountPercent;
            shipping = rule.Shipping ?? shipping;
            applied.Add(rule.Shipping.HasValue ? $"shipping {rule.Shipping}" : $"discount {rule.DiscountPercent}");
        }
        percent = Math.Min(percent, 50);
        decimal discount = Math.Round(order.Subtotal * percent / 100m, 2);
        return new PriceQuoteModel(order.Subtotal, percent, discount, shipping, order.Subtotal - discount + shipping, applied);
    }
}
