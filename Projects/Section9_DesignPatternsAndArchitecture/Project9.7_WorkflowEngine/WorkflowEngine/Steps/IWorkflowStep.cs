using WorkflowEngine.Contexts;

namespace WorkflowEngine.Steps;

// One step of a workflow. The ENGINE decides when to run it, retry it, skip it or undo it;
// the step only knows how to do (and undo) its own piece of work.
public interface IWorkflowStep
{
    string Name { get; }

    int MaximumAttempts { get; }

    bool ShouldRun(WorkflowContext context);

    Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken);

    // COMPENSATION: undo this step's effect when a LATER step fails (release stock, refund money).
    // Distributed work can't use a database transaction across services, so this is how it "rolls back".
    Task CompensateAsync(WorkflowContext context);
}
