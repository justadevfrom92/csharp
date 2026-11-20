using Iterators.Generators;

namespace Iterators.Tests;

public class OrderNumberGeneratorTest
{
    [Fact]
    public void Generate_PositivePositive_RunsOneStepPerItem_OnlyWhenLooped()
    {
        var log = new List<string>();

        IEnumerable<string> orders = OrderNumberGenerator.Generate(2, log);
        Assert.Empty(log); // nothing ran yet
        foreach (string order in orders)
        {
            log.Add("used " + order);
        }

        Assert.Equal(["made 1", "used ORD-0001", "made 2", "used ORD-0002"], log); // made and used take turns
    }

    [Fact]
    public void Generate_PositiveNegative_TakeStopsEarly_LoopingTwiceRunsTwice()
    {
        var log = new List<string>();
        IEnumerable<string> orders = OrderNumberGenerator.Generate(100, log);

        Assert.Equal(["ORD-0001", "ORD-0002"], orders.Take(2));
        Assert.Equal(2, log.Count);            // items 3..100 were never made
        Assert.Equal(100, orders.Count());
        Assert.Equal(102, log.Count);          // Count() started again from 1
        Assert.Equal([1, 2, 3], OrderNumberGenerator.Forever().Take(3));
    }

    [Fact]
    public void Generate_NegativeNegative_ZeroOrNegativeCount_IsJustEmpty()
    {
        Assert.Empty(OrderNumberGenerator.Generate(0));
        Assert.Empty(OrderNumberGenerator.Generate(-5));
    }

    [Fact]
    public void YieldBreak_PositivePositive_StopsAtTheBlankLine()
    {
        Assert.Equal(["a", "b"], OrderNumberGenerator.UntilBlankLine(["a", "b", " ", "c"]));
    }

    [Fact]
    public void YieldBreak_PositiveNegative_NoBlankLine_GivesEverything_BlankFirst_GivesNothing()
    {
        Assert.Equal(["a", "b"], OrderNumberGenerator.UntilBlankLine(["a", "b"]));
        Assert.Empty(OrderNumberGenerator.UntilBlankLine(["", "a"]));
    }

    [Fact]
    public void YieldBreak_NegativeNegative_NullInput_ThrowsOnlyWhenLooped()
    {
        IEnumerable<string> lines = OrderNumberGenerator.UntilBlankLine(null!); // no error yet

        Assert.Throws<NullReferenceException>(() => lines.ToList());
    }

    [Fact]
    public void GenerateChecked_PositivePositive_SameItemsAsGenerate()
    {
        Assert.Equal(OrderNumberGenerator.Generate(3), OrderNumberGenerator.GenerateChecked(3));
    }

    [Fact]
    public void GenerateChecked_PositiveNegative_ZeroIsAllowed()
    {
        Assert.Empty(OrderNumberGenerator.GenerateChecked(0));
    }

    [Fact]
    public void GenerateChecked_NegativeNegative_NegativeCount_ThrowsAtTheCall_NotAtTheLoop()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderNumberGenerator.GenerateChecked(-1));
    }
}
