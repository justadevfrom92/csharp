using PipelinePatterns.Models;

namespace PipelinePatterns.Handlers;

// CHAIN OF RESPONSIBILITY: a request is passed along a chain of handlers. Each one either HANDLES it
// (and the chain stops) or PASSES it to the next. The sender doesn't know who will finally deal with it.
//
//   team lead (up to 1,000) -> manager (up to 10,000) -> director (up to 100,000) -> nobody: rejected
//
// Difference from pipes and filters: there EVERY filter runs; here only ONE handler deals with the request.
public abstract class BaseApprovalHandler
{
    private BaseApprovalHandler? next;

    // Returns the handler you passed in, so a chain can be built in one line:
    //   teamLead.SetNext(manager).SetNext(director);
    public BaseApprovalHandler SetNext(BaseApprovalHandler nextHandler)
    {
        ArgumentNullException.ThrowIfNull(nextHandler);
        next = nextHandler;
        return nextHandler;
    }

    public string Approve(ExpenseModel expense)
    {
        ArgumentNullException.ThrowIfNull(expense);
        if (expense.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expense), "Amount must be more than 0.");
        }
        if (CanApprove(expense))
        {
            return $"Approved by {Role}";
        }
        if (next == null)
        {
            return $"Rejected: {expense.Amount:0.00} is more than anyone can approve";
        }
        return next.Approve(expense);
    }

    protected abstract string Role { get; }

    protected abstract bool CanApprove(ExpenseModel expense);
}
