using WorkflowEngine.Outcomes;

namespace WorkflowEngine.Models;

public record WorkflowResultModel(WorkflowOutcome Outcome, string? FailedStep, string? Error);
