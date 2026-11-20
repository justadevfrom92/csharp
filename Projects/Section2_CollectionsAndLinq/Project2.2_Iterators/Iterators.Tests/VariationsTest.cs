using Iterators.Collections;
using Iterators.Generators;
using Iterators.Variations;

namespace Iterators.Tests;

public class VariationsTest
{
    [Fact]
    public void Shelf_PositivePositive_YieldVersionGivesTheSameItems()
    {
        var byHand = new ShelfCollection { "Tea", "Coffee" };
        var withYield = new ShelfYieldVariation { "Tea", "Coffee" };

        Assert.Equal(byHand, withYield);
    }

    [Fact]
    public void Shelf_PositiveNegative_AddDuringLoop_YieldVersionThrows_ByHandCopyDoesNot()
    {
        var withYield = new ShelfYieldVariation { "Tea" };

        Assert.Throws<InvalidOperationException>(() =>
        {
            foreach (string _ in withYield)
            {
                withYield.Add("Milk"); // "Collection was modified" from List<T>
            }
        });
    }

    [Fact]
    public void Shelf_NegativeNegative_BlankItem_BothReject()
    {
        Assert.Throws<ArgumentException>(() => new ShelfYieldVariation { "" });
        Assert.Throws<ArgumentException>(() => new ShelfCollection { "" });
    }

    [Fact]
    public void LateCheck_PositivePositive_ValidCount_SameAsChecked()
    {
        Assert.Equal(OrderNumberGenerator.GenerateChecked(2), LateCheckVariation.Generate(2));
    }

    [Fact]
    public void LateCheck_PositiveNegative_BadCall_DoesNotThrowUntilLooped()
    {
        IEnumerable<string> orders = LateCheckVariation.Generate(-1); // no exception here

        Assert.Throws<ArgumentOutOfRangeException>(() => orders.First());
    }

    [Fact]
    public void LateCheck_NegativeNegative_CheckedVersion_ThrowsAtTheCall()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderNumberGenerator.GenerateChecked(-1));
    }

    [Fact]
    public void MultipleEnumeration_PositivePositive_SameAnswerBothWays()
    {
        Assert.Equal((2, 2), MultipleEnumerationVariation.TwiceOverQuery(new int[1]));
        Assert.Equal((2, 2), MultipleEnumerationVariation.OnceWithToList(new int[1]));
    }

    [Fact]
    public void MultipleEnumeration_PositiveNegative_QueryReadsTheSourceTwice_ToListOnce()
    {
        int[] queryRuns = new int[1];
        int[] listRuns = new int[1];

        MultipleEnumerationVariation.TwiceOverQuery(queryRuns);
        MultipleEnumerationVariation.OnceWithToList(listRuns);

        Assert.Equal(2, queryRuns[0]);
        Assert.Equal(1, listRuns[0]);
    }

    [Fact]
    public void MultipleEnumeration_NegativeNegative_JustBuildingTheQuery_ReadsNothing()
    {
        int[] runs = new int[1];

        IEnumerable<int> query = MultipleEnumerationVariation.Source(runs).Where(number => number > 2);

        Assert.Equal(0, runs[0]);
        Assert.NotNull(query);
    }
}
