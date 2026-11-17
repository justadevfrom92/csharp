using PipelinePatterns.Models;

namespace PipelinePatterns.Variations;

// The approval chain as a TABLE + First(), instead of linked handler objects (Handlers/, original).
// Same answers for "the first one whose limit is high enough". The chain of objects is better when a handler does
// more than compare a number (calls a service, asks a person, can PARTLY handle and pass on).
public static class ApprovalTableVariation
{
    private static readonly (decimal Limit, string Role)[] approvers =
    [
        (1_000m, "team lead"),
        (10_000m, "manager"),
        (100_000m, "director"),
    ];

    public static string Approve(ExpenseModel expense)
    {
        ArgumentNullException.ThrowIfNull(expense);
        if (expense.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expense), "Amount must be more than 0.");
        }
        foreach ((decimal limit, string role) in approvers)
        {
            if (expense.Amount <= limit)
            {
                return $"Approved by {role}";
            }
        }
        return $"Rejected: {expense.Amount:0.00} is more than anyone can approve";
    }
}
