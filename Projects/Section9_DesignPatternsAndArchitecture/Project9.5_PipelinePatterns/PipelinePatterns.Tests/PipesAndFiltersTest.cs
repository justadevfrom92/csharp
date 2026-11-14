using PipelinePatterns.Filters;
using PipelinePatterns.Pipelines;

namespace PipelinePatterns.Tests;

// PositivePositive = valid input -> works; PositiveNegative = well-formed input that breaks a rule -> refused/unchanged;
// NegativeNegative = broken or missing input -> rejected.
public class PipesAndFiltersTest
{
    private static TextPipeline CleanLogLine()
    {
        return new TextPipeline()
            .Add(new TrimWhitespaceFilter())
            .Add(new MaskEmailFilter())
            .Add(new MaskCardNumberFilter())
            .Add(new MaximumLengthFilter(40));
    }

    [Fact]
    public void Run_PositivePositive_EveryFilterRunsInOrder()
    {
        string result = CleanLogLine().Run("  paid by  ann@example.com card 4111-1111-1111-1234 ");

        Assert.Equal("paid by [email] card **** 1234", result);
    }

    [Fact]
    public void Run_PositiveNegative_NothingToClean_TextComesOutUnchanged_AndLongTextIsCut()
    {
        Assert.Equal("all good", CleanLogLine().Run("all good"));
        Assert.Equal(new string('x', 37) + "...", CleanLogLine().Run(new string('x', 100)));
        Assert.Equal("anything", new TextPipeline().Run("anything")); // no filters = no change
    }

    [Fact]
    public void Run_NegativeNegative_NullTextOrFilter_OrBadLength_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CleanLogLine().Run(null!));
        Assert.Throws<ArgumentNullException>(() => new TextPipeline().Add(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MaximumLengthFilter(3));
    }
}
