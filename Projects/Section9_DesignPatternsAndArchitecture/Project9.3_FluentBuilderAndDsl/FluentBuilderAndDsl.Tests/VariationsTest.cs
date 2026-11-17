using FluentBuilderAndDsl.Builders;
using FluentBuilderAndDsl.Evaluators;
using FluentBuilderAndDsl.Models;
using FluentBuilderAndDsl.Parsers;
using FluentBuilderAndDsl.Variations;

namespace FluentBuilderAndDsl.Tests;

// Builder and DSL variations (FluentBuilderAndDsl/Variations/) compared with OrderBuilder and the text DSL.
public class VariationsTest
{
    [Fact]
    public void Building_PositivePositive_InitializerAndImmutableBuilder_MakeTheSameOrder()
    {
        OrderModel original = new OrderBuilder().ForCustomer("Ann").AddLine("KB-01", 2, 25m).Build();
        OrderModel immutable = new ImmutableOrderBuilderVariation().ForCustomer("Ann").AddLine("KB-01", 2, 25m).Build();
        var initialized = new OrderInitVariation { Customer = "Ann", Lines = [new OrderLineModel("KB-01", 2, 25m)] };

        Assert.Equal(original.Subtotal, immutable.Subtotal);
        Assert.Equal(original.Lines, immutable.Lines);
        Assert.Equal(original.Lines, initialized.Lines);
        Assert.Equal("US", initialized.Country);
    }

    [Fact]
    public void Building_PositiveNegative_ReusingAHalfBuiltBuilder_MutableLeaks_ImmutableDoesNot()
    {
        OrderBuilder mutableForAnn = new OrderBuilder().ForCustomer("Ann");
        OrderModel mutableA = mutableForAnn.AddLine("KB-01", 1, 25m).Build();
        OrderModel mutableB = mutableForAnn.AddLine("MS-01", 1, 12.5m).Build();

        ImmutableOrderBuilderVariation immutableForAnn = new ImmutableOrderBuilderVariation().ForCustomer("Ann");
        OrderModel immutableA = immutableForAnn.AddLine("KB-01", 1, 25m).Build();
        OrderModel immutableB = immutableForAnn.AddLine("MS-01", 1, 12.5m).Build();

        Assert.Single(mutableA.Lines);
        Assert.Equal(2, mutableB.Lines.Count);    // order B ALSO got the keyboard: both steps changed the same builder
        Assert.Single(immutableA.Lines);
        Assert.Single(immutableB.Lines);          // each branch is independent
    }

    [Fact]
    public void Building_PositiveNegative_InitializerChecksPresenceOnly_EmptyLinesAccepted()
    {
        var empty = new OrderInitVariation { Customer = "Ann", Lines = [] }; // compiles and runs

        Assert.Empty(empty.Lines);
        Assert.Throws<InvalidOperationException>(() => new OrderBuilder().ForCustomer("Ann").Build()); // the builder refuses
    }

    [Fact]
    public void Dsl_PositivePositive_InternalAndExternalDsl_GiveTheSameQuote()
    {
        OrderModel order = new OrderBuilder().ForCustomer("Ann").ShipTo("CA").AddLine("KB-01", 5, 30m).Build();
        List<PricingRuleModel> textRules = PricingRuleParser.Parse("WHEN total > 100 THEN discount 10%\nWHEN items >= 5 THEN discount 5%\nWHEN country = CA THEN shipping 4.99");
        FluentRuleVariation[] codeRules =
        [
            FluentRuleVariation.When(o => o.Subtotal > 100).Discount(10),
            FluentRuleVariation.When(o => o.ItemCount >= 5).Discount(5),
            FluentRuleVariation.When(o => o.Country == "CA").ShipFor(4.99m),
        ];

        PriceQuoteModel fromText = PricingRuleEvaluator.Quote(order, textRules);
        PriceQuoteModel fromCode = FluentRuleVariation.Quote(order, codeRules);

        Assert.Equal(fromText.Total, fromCode.Total);
        Assert.Equal(fromText.DiscountPercent, fromCode.DiscountPercent);
    }

    [Fact]
    public void Dsl_NegativeNegative_MistakesAreFoundAtDifferentTimes()
    {
        // External DSL: a typo is found when the FILE is read (at runtime), with the line number.
        Assert.Throws<FormatException>(() => PricingRuleParser.Parse("WHEN totl > 100 THEN discount 10%"));
        // Internal DSL: "o.Subtotl" wouldn't COMPILE at all; a null condition is the only runtime mistake left.
        Assert.Throws<ArgumentNullException>(() => FluentRuleVariation.When(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImmutableOrderBuilderVariation().AddLine("KB-01", 0, 1m));
    }
}
