using System.Text.Json;
using Tuples.Calculators;
using Tuples.Variations;

namespace Tuples.Tests;

public class VariationsTest
{
    private static readonly List<decimal> Prices = [19.99m, 5.01m, 25.00m];

    [Fact]
    public void MultipleResults_PositivePositive_AllWaysGiveTheSameNumbers()
    {
        MultipleResultsVariation.WithOut(Prices, out decimal total, out int count);
        Tuple<decimal, int> tupleClass = MultipleResultsVariation.WithTupleClass(Prices);
        (decimal, int) unnamed = MultipleResultsVariation.WithUnnamedTuple(Prices);
        var (recordTotal, recordCount) = MultipleResultsVariation.WithRecord(Prices); // records deconstruct too

        Assert.Equal((50.00m, 3), (total, count));
        Assert.Equal((50.00m, 3), (tupleClass.Item1, tupleClass.Item2));
        Assert.Equal((50.00m, 3), (unnamed.Item1, unnamed.Item2));
        Assert.Equal((50.00m, 3), (recordTotal, recordCount));
        Assert.Equal((PriceStatsCalculator.Summarize(Prices).Total, 3), unnamed);
    }

    [Fact]
    public void MultipleResults_PositiveNegative_RecordAndTupleClassAreObjects_AnonymousOnlyReadableByReflection()
    {
        object anonymous = MultipleResultsVariation.WithAnonymousType(Prices);

        Assert.False(typeof((decimal, int)).IsClass);                     // value tuple: a struct
        Assert.True(typeof(Tuple<decimal, int>).IsClass);                 // Tuple<>: a class
        Assert.Equal(new PriceTotalsVariation(50.00m, 3), MultipleResultsVariation.WithRecord(Prices)); // equal by value
        Assert.Equal(50.00m, anonymous.GetType().GetProperty("Total")!.GetValue(anonymous));
    }

    [Fact]
    public void MultipleResults_NegativeNegative_NullList_ThrowsInEveryWay()
    {
        Assert.Throws<ArgumentNullException>(() => MultipleResultsVariation.WithOut(null!, out _, out _));
        Assert.Throws<ArgumentNullException>(() => MultipleResultsVariation.WithTupleClass(null!));
        Assert.Throws<ArgumentNullException>(() => MultipleResultsVariation.WithRecord(null!));
    }

    [Fact]
    public void Parcel_PositivePositive_RecordDeconstructsLikeTheHandWrittenClass()
    {
        var (weight, destination) = new ParcelRecordVariation(2.5m, "Paris");

        Assert.Equal((2.5m, "Paris"), (weight, destination));
    }

    [Fact]
    public void Gotchas_PositivePositive_RecordKeepsItsNamesInJson()
    {
        Assert.Equal("{\"Total\":50.00,\"Count\":3}", JsonSerializer.Serialize(MultipleResultsVariation.WithRecord(Prices)));
    }

    [Fact]
    public void Gotchas_PositiveNegative_JsonLosesTupleNames()
    {
        Assert.Equal("{}", TupleGotchaVariation.ToJson(("Tea", 12)));
        Assert.Equal("{\"Item1\":\"Tea\",\"Item2\":12}", TupleGotchaVariation.ToJsonIncludingFields(("Tea", 12)));
    }

    [Fact]
    public void Gotchas_PositiveNegative_TuplesAreMutableCopies_EqualityIgnoresNames()
    {
        Assert.Equal((1, 99), TupleGotchaVariation.ChangeACopy());
        Assert.True(TupleGotchaVariation.SameByPosition()); // (Width 3, Height 4) == (Count 3, Total 4)
    }

    [Fact]
    public void Gotchas_NegativeNegative_TupleClassCannotBeChanged()
    {
        Assert.Null(typeof(Tuple<int, int>).GetProperty("Item1")!.SetMethod); // read-only property, unlike ValueTuple's public field
        Assert.NotNull(typeof(ValueTuple<int, int>).GetField("Item1"));
    }
}
