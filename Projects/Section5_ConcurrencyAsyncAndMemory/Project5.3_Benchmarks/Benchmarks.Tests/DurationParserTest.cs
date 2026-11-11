using Benchmarks.Parsers;

namespace Benchmarks.Tests;

// A benchmark is only fair if both versions do EXACTLY the same job, so every test checks both.
// PositivePositive - valid line -> same number from both
// PositiveNegative - well-formed line with a negative duration -> both refuse
// NegativeNegative - null / malformed line -> both reject
public class DurationParserTest
{
    [Theory]
    [InlineData("2026-11-05T10:00:00Z ERROR 1234 /api/orders", 1234)]
    [InlineData("2026-11-05T10:00:00Z INFO 0 /", 0)]
    [InlineData("t WARN 7 /x extra words", 7)]
    public void Parse_PositivePositive_ValidLine_BothVersionsReturnSameDuration(string line, int expected)
    {
        Assert.Equal(expected, DurationParser.ParseWithSplit(line));
        Assert.Equal(expected, DurationParser.ParseWithSpan(line));
    }

    [Fact]
    public void Parse_PositiveNegative_NegativeDuration_BothVersionsRefuse()
    {
        const string line = "2026-11-05T10:00:00Z ERROR -5 /api/orders";

        Assert.Throws<ArgumentOutOfRangeException>(() => DurationParser.ParseWithSplit(line));
        Assert.Throws<ArgumentOutOfRangeException>(() => DurationParser.ParseWithSpan(line));
    }

    [Theory]
    [InlineData("")]
    [InlineData("only two")]
    [InlineData("t ERROR 1234")]                 // no path after the number
    [InlineData("t ERROR twelve /api/orders")]  // not a number
    public void Parse_NegativeNegative_MalformedLine_BothVersionsReject(string line)
    {
        Assert.Throws<FormatException>(() => DurationParser.ParseWithSplit(line));
        Assert.Throws<FormatException>(() => DurationParser.ParseWithSpan(line));
    }

    [Fact]
    public void Parse_NegativeNegative_Null_BothVersionsReject()
    {
        Assert.Throws<ArgumentNullException>(() => DurationParser.ParseWithSplit(null!));
        Assert.Throws<ArgumentNullException>(() => DurationParser.ParseWithSpan(null!));
    }
}
