using PipelinePatterns.Models;

namespace PipelinePatterns.Handlers;

public class TeamLeadApprovalHandler : BaseApprovalHandler
{
    public const decimal Limit = 1_000m;

    protected override string Role => "team lead";

    protected override bool CanApprove(ExpenseModel expense)
    {
        return expense.Amount <= Limit;
    }
}
