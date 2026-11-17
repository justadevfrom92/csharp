using OperatorsAndLambdas.ValueObjects;
using OperatorsAndLambdas.Variations;

namespace OperatorsAndLambdas.Tests;

public class VariationsTest
{
    [Fact]
    public void Delegates_PositivePositive_AllFiveSyntaxesDoTheSame()
    {
        Assert.Equal(14, DelegateSyntaxVariation.AsLambda()(7));
        Assert.Equal(14, DelegateSyntaxVariation.AsAnonymousMethod()(7));
        Assert.Equal(14, DelegateSyntaxVariation.AsLocalFunction()(7));
        Assert.Equal(14, DelegateSyntaxVariation.AsMethodGroup()(7));
        Assert.Equal(14, DelegateSyntaxVariation.AsCustomDelegate()(7));
    }

    [Fact]
    public void Closures_PositiveNegative_ForLoopSharesOneVariable_CopyAndForeachDoNot()
    {
        Assert.Equal([3, 3, 3], LoopCaptureVariation.WithForLoop());
        Assert.Equal([0, 1, 2], LoopCaptureVariation.WithForLoopAndCopy());
        Assert.Equal([0, 1, 2], LoopCaptureVariation.WithForeachLoop());
    }

    [Fact]
    public void Conversions_PositiveNegative_ImplicitGuessesTheCurrency_OriginalRefuses()
    {
        MoneyImplicitVariation price = 19.99m; // compiles: silently becomes USD

        Assert.Equal("USD", price.Currency);
        Assert.Equal(new MoneyImplicitVariation(25m, "USD"), price.Add(5.01m)); // even the argument converts silently
        // MoneyValueObject has no implicit conversion: "MoneyValueObject m = 19.99m;" does not compile.
        Assert.Equal(19.99m, (decimal)new MoneyValueObject(19.99m, "USD"));     // only the explicit cast OUT exists
    }

    [Fact]
    public void Conversions_NegativeNegative_NamedMethodAndOperator_BothRejectMixedCurrencies()
    {
        Assert.Throws<InvalidOperationException>(() => new MoneyImplicitVariation(5m, "EUR").Add(5m));   // 5m became USD
        Assert.Throws<InvalidOperationException>(() => new MoneyValueObject(5m, "EUR") + new MoneyValueObject(5m, "USD"));
    }
}
