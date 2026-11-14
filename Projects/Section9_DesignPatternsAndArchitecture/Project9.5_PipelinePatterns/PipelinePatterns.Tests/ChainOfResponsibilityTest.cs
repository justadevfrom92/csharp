using PipelinePatterns.Handlers;
using PipelinePatterns.Models;

namespace PipelinePatterns.Tests;

public class ChainOfResponsibilityTest
{
    private static BaseApprovalHandler Chain()
    {
        var teamLead = new TeamLeadApprovalHandler();
        teamLead.SetNext(new ManagerApprovalHandler()).SetNext(new DirectorApprovalHandler());
        return teamLead;
    }

    [Theory]
    [InlineData(0.01, "team lead")]
    [InlineData(1000, "team lead")]      // limits are inclusive
    [InlineData(1000.01, "manager")]
    [InlineData(10000, "manager")]
    [InlineData(100000, "director")]
    public void Approve_PositivePositive_FirstHandlerWithEnoughLimitApproves(decimal amount, string expectedRole)
    {
        Assert.Equal($"Approved by {expectedRole}", Chain().Approve(new ExpenseModel("Ann", amount, "x")));
    }

    [Fact]
    public void Approve_PositiveNegative_MoreThanAnyLimit_OrChainTooShort_IsRejected()
    {
        Assert.StartsWith("Rejected", Chain().Approve(new ExpenseModel("Ann", 100_000.01m, "x")));
        Assert.StartsWith("Rejected", new TeamLeadApprovalHandler().Approve(new ExpenseModel("Ann", 5_000m, "x"))); // no next handler
    }

    [Fact]
    public void Approve_NegativeNegative_ZeroAmountOrNulls_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Chain().Approve(new ExpenseModel("Ann", 0m, "x")));
        Assert.Throws<ArgumentNullException>(() => Chain().Approve(null!));
        Assert.Throws<ArgumentNullException>(() => new TeamLeadApprovalHandler().SetNext(null!));
    }
}
