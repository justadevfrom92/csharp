using PipelinePatterns.Models;

namespace PipelinePatterns.Handlers;

public class DirectorApprovalHandler : BaseApprovalHandler
{
    public const decimal Limit = 100_000m;

    protected override string Role => "director";

    protected override bool CanApprove(ExpenseModel expense)
    {
        return expense.Amount <= Limit;
    }
}
