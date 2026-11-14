using FluentBuilderAndDsl.Builders;
using FluentBuilderAndDsl.Models;

namespace FluentBuilderAndDsl.Tests;

// PositivePositive = valid input -> works; PositiveNegative = well-formed input that breaks a rule -> refused;
// NegativeNegative = broken or missing input -> rejected.
public class OrderBuilderTest
{
    [Fact]
    public void Build_PositivePositive_ChainedSteps_CreateTheOrder()
    {
        OrderModel order = new OrderBuilder()
            .ForCustomer("  Ann ")
            .ShipTo("ca")
            .AddLine("KB-01", 2, 25m)
            .AddLine("MS-01", 1, 12.5m)
            .WithNote("Ring twice")
            .Build();

        Assert.Equal("Ann", order.Customer);
        Assert.Equal("CA", order.Country);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(62.5m, order.Subtotal);
        Assert.Equal(3, order.ItemCount);
        Assert.Equal("Ring twice", order.Note);
    }

    [Fact]
    public void Build_PositivePositive_OptionalStepsLeftOut_UseDefaults()
    {
        OrderModel order = new OrderBuilder().ForCustomer("Ann").AddLine("KB-01", 1, 25m).Build();

        Assert.Equal("US", order.Country);
        Assert.Null(order.Note);
    }

    [Fact]
    public void Build_PositiveNegative_MissingCustomerOrLines_IsRefusedAtBuild()
    {
        var noCustomer = new OrderBuilder().AddLine("KB-01", 1, 25m);
        var noLines = new OrderBuilder().ForCustomer("Ann");

        Assert.Throws<InvalidOperationException>(() => noCustomer.Build());
        Assert.Throws<InvalidOperationException>(() => noLines.Build());
    }

    [Fact]
    public void Build_PositiveNegative_ChangingBuilderAfterBuild_DoesNotChangeTheOrder()
    {
        var builder = new OrderBuilder().ForCustomer("Ann").AddLine("KB-01", 1, 25m);
        OrderModel first = builder.Build();

        builder.AddLine("MS-01", 1, 12.5m);

        Assert.Single(first.Lines); // Build() copied the lines
    }

    [Theory]
    [InlineData("KB-01", 0, 25)]
    [InlineData("KB-01", -1, 25)]
    [InlineData("KB-01", 1, 0)]
    public void AddLine_NegativeNegative_BadQuantityOrPrice_Throws(string sku, int quantity, decimal price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderBuilder().AddLine(sku, quantity, price));
    }

    [Fact]
    public void Steps_NegativeNegative_MissingOrMalformedText_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => new OrderBuilder().ForCustomer(""));
        Assert.ThrowsAny<ArgumentException>(() => new OrderBuilder().ForCustomer(null!));
        Assert.ThrowsAny<ArgumentException>(() => new OrderBuilder().ShipTo("USA"));
        Assert.ThrowsAny<ArgumentException>(() => new OrderBuilder().AddLine(" ", 1, 1m));
    }
}
