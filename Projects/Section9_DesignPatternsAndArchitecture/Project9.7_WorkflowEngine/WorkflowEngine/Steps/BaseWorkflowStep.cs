using WorkflowEngine.Contexts;

namespace WorkflowEngine.Steps;

// Sensible defaults, so each step only writes what's special about it.
public abstract class BaseWorkflowStep : IWorkflowStep
{
    public abstract string Name { get; }

    public virtual int MaximumAttempts => 1;

    public virtual bool ShouldRun(WorkflowContext context)
    {
        return true;
    }

    public abstract Task ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken);

    public virtual Task CompensateAsync(WorkflowContext context)
    {
        return Task.CompletedTask; // nothing to undo
    }
}
