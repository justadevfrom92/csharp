using Benchmarks.Builders;

namespace Benchmarks.Tests;

// PositivePositive - values -> "a,b,c" from all three versions
// PositiveNegative - a valid but EMPTY list -> "" (nothing to join)
// NegativeNegative - null -> rejected by all three
public class CsvLineBuilderTest
{
    private static readonly Func<IReadOnlyList<string>, string>[] allVersions =
    [
        CsvLineBuilder.BuildWithPlusEquals,
        CsvLineBuilder.BuildWithStringBuilder,
        CsvLineBuilder.BuildWithJoin,
    ];

    [Fact]
    public void Build_PositivePositive_Values_AllVersionsReturnSameLine()
    {
        string[] values = ["a", "b", "c"];

        foreach (Func<IReadOnlyList<string>, string> build in allVersions)
        {
            Assert.Equal("a,b,c", build(values));
        }
    }

    [Fact]
    public void Build_PositivePositive_ThousandValues_AllVersionsAgree()
    {
        List<string> values = Enumerable.Range(0, 1000).Select(number => "value" + number).ToList();

        string expected = CsvLineBuilder.BuildWithJoin(values);
        Assert.Equal(expected, CsvLineBuilder.BuildWithPlusEquals(values));
        Assert.Equal(expected, CsvLineBuilder.BuildWithStringBuilder(values));
    }

    [Fact]
    public void Build_PositiveNegative_EmptyList_AllVersionsReturnEmptyString()
    {
        foreach (Func<IReadOnlyList<string>, string> build in allVersions)
        {
            Assert.Equal("", build([]));
        }
    }

    [Fact]
    public void Build_NegativeNegative_Null_AllVersionsReject()
    {
        foreach (Func<IReadOnlyList<string>, string> build in allVersions)
        {
            Assert.Throws<ArgumentNullException>(() => build(null!));
        }
    }
}
