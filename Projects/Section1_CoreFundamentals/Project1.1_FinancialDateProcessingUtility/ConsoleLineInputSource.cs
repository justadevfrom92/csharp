namespace FinancialDateProcessingUtility;

/// <summary>
/// Abstraction over "where the next line of user input comes from". The interactive mode reads
/// from the console; the --demo mode replays a scripted list so the tool can be executed
/// unattended (e.g. in CI) while running exactly the same code paths.
/// </summary>
public interface IConsoleLineInputSource
{
    string? ReadNextLine(string promptText);
}

public sealed class InteractiveConsoleLineInputSource : IConsoleLineInputSource
{
    public string? ReadNextLine(string promptText)
    {
        Console.Write(promptText);
        return Console.ReadLine(); // Returns null on end-of-stream; callers must handle it.
    }
}

public sealed class ScriptedConsoleLineInputSource(IEnumerable<string?> scriptedLines) : IConsoleLineInputSource
{
    private readonly Queue<string?> _remainingScriptedLines = new(scriptedLines);

    public string? ReadNextLine(string promptText)
    {
        string? nextLine = _remainingScriptedLines.Count > 0 ? _remainingScriptedLines.Dequeue() : null;
        Console.WriteLine($"{promptText}{nextLine ?? "<end of input>"}");
        return nextLine;
    }
}
