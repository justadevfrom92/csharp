using PipelinePatterns.Models;

namespace PipelinePatterns.Handlers;

public class ManagerApprovalHandler : BaseApprovalHandler
{
    public const decimal Limit = 10_000m;

    protected override string Role => "manager";

    protected override bool CanApprove(ExpenseModel expense)
    {
        return expense.Amount <= Limit;
    }
}
