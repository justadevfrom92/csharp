namespace WorkflowEngine.Outcomes;

public enum WorkflowOutcome
{
    Completed,
    RolledBack,   // a step failed for good; every finished step was compensated (undone)
}
