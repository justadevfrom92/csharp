using System.Text.Json;

namespace WorkflowEngine.Contexts;

// Everything ONE run of a workflow knows: its input values, which steps are done, and what happened.
// It can be saved as JSON after every step, so a run that crashed half-way can be RESUMED later
// (finished steps are skipped instead of being done twice).
public class WorkflowContext
{
    public string WorkflowId { get; set; } = Guid.NewGuid().ToString("N");

    public Dictionary<string, string> Values { get; set; } = new();

    public List<string> CompletedSteps { get; set; } = new();

    public List<string> History { get; set; } = new();

    public string Get(string key)
    {
        return Values.TryGetValue(key, out string? value) ? value : throw new KeyNotFoundException($"Workflow value '{key}' is missing.");
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    public static WorkflowContext FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<WorkflowContext>(json) ?? throw new JsonException("Empty workflow state.");
    }
}
