using System.Globalization;
using FluentBuilderAndDsl.Models;

namespace FluentBuilderAndDsl.Evaluators;

// Runs parsed DSL rules against an order.
//   discount: all matching discounts ADD UP, capped at 50%.
//   shipping: the LAST matching shipping rule sets the price (default 9.99).
public static class PricingRuleEvaluator
{
    public const decimal DefaultShipping = 9.99m;
    public const int MaximumDiscountPercent = 50;

    public static PriceQuoteModel Quote(OrderModel order, IEnumerable<PricingRuleModel> rules)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(rules);

        int discountPercent = 0;
        decimal shipping = DefaultShipping;
        var applied = new List<string>();

        foreach (PricingRuleModel rule in rules)
        {
            if (!Matches(order, rule))
            {
                continue;
            }
            applied.Add($"line {rule.LineNumber}: {rule.Action} {rule.Amount}");
            if (rule.Action == "discount")
            {
                discountPercent += (int)rule.Amount;
            }
            else
            {
                shipping = rule.Amount;
            }
        }

        discountPercent = Math.Min(discountPercent, MaximumDiscountPercent);
        decimal discount = Math.Round(order.Subtotal * discountPercent / 100m, 2);
        decimal total = order.Subtotal - discount + shipping;
        return new PriceQuoteModel(order.Subtotal, discountPercent, discount, shipping, total, applied);
    }

    private static bool Matches(OrderModel order, PricingRuleModel rule)
    {
        if (rule.Field == "country")
        {
            return order.Country == rule.Value;
        }
        decimal actual = rule.Field == "total" ? order.Subtotal : order.ItemCount;
        decimal expected = decimal.Parse(rule.Value, CultureInfo.InvariantCulture);
        return rule.Operator switch
        {
            ">" => actual > expected,
            ">=" => actual >= expected,
            "<" => actual < expected,
            "<=" => actual <= expected,
            _ => actual == expected,
        };
    }
}
