using OperatorsAndLambdas.Helpers;
using OperatorsAndLambdas.Models;

namespace OperatorsAndLambdas.Tests;

public class LambdaHelperTest
{
    private static readonly List<ProductModel> products =
    [
        new ProductModel("Keyboard", 25m, 10),
        new ProductModel("Mouse", 12.5m, 0),
        new ProductModel("Monitor", 180m, 3),
    ];

    // ---------- MakeMultiplier (closure) ----------

    [Fact]
    public void MakeMultiplier_PositivePositive_RemembersFactorAfterReturning()
    {
        Func<decimal, decimal> addTax = LambdaHelper.MakeMultiplier(1.2m);
        Func<decimal, decimal> half = LambdaHelper.MakeMultiplier(0.5m);

        Assert.Equal(120m, addTax(100m));
        Assert.Equal(50m, half(100m)); // each closure keeps its OWN captured value
    }

    [Fact]
    public void MakeMultiplier_PositiveNegative_ZeroOrNegativeFactor_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LambdaHelper.MakeMultiplier(0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => LambdaHelper.MakeMultiplier(-1m));
    }

    [Fact]
    public void MakeMultiplier_NegativeNegative_ResultTooBigForDecimal_ThrowsOverflow()
    {
        Func<decimal, decimal> doubler = LambdaHelper.MakeMultiplier(2m);

        Assert.Throws<OverflowException>(() => doubler(decimal.MaxValue)); // decimal never wraps silently
    }

    // ---------- Compose / ApplyAll ----------

    [Fact]
    public void Compose_PositivePositive_RunsFirstThenSecond()
    {
        Func<int, int> addOneThenDouble = LambdaHelper.Compose<int>(value => value + 1, value => value * 2);

        Assert.Equal(8, addOneThenDouble(3)); // (3 + 1) * 2, not 3 * 2 + 1
        Assert.Equal(108m, LambdaHelper.ApplyAll(100m, [LambdaHelper.MakeMultiplier(0.9m), LambdaHelper.MakeMultiplier(1.2m)]));
    }

    [Fact]
    public void Compose_PositiveNegative_NoRules_ReturnsPriceUnchangedButNegativePriceThrows()
    {
        Assert.Equal(100m, LambdaHelper.ApplyAll(100m, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => LambdaHelper.ApplyAll(-1m, []));
    }

    [Fact]
    public void Compose_NegativeNegative_NullFunctions_ThrowArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => LambdaHelper.Compose<int>(null!, value => value));
        Assert.Throws<ArgumentNullException>(() => LambdaHelper.Compose<int>(value => value, null!));
        Assert.Throws<ArgumentNullException>(() => LambdaHelper.ApplyAll(1m, null!));
    }

    // ---------- Expression trees ----------

    [Fact]
    public void Expression_PositivePositive_CanBeReadAndRun()
    {
        string text = LambdaHelper.Describe(product => product.Price > 20);
        List<ProductModel> found = LambdaHelper.Filter(products, product => product.Price > 20 && product.Stock > 0);

        Assert.Equal("(product.Price > 20)", text);
        Assert.Equal(["Keyboard", "Monitor"], found.Select(product => product.Name));
    }

    [Fact]
    public void Expression_PositiveNegative_NothingMatches_ReturnsEmptyList()
    {
        Assert.Empty(LambdaHelper.Filter(products, product => product.Price > 1000));
    }

    [Fact]
    public void Expression_NegativeNegative_NullArguments_ThrowArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => LambdaHelper.Describe(null!));
        Assert.Throws<ArgumentNullException>(() => LambdaHelper.Filter(null!, product => true));
        Assert.Throws<ArgumentNullException>(() => LambdaHelper.Filter(products, null!));
    }
}
