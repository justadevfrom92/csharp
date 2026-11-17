namespace BehavioralPatterns.Variations;

// Undo/redo by SNAPSHOTS ("memento"): save the WHOLE text before each change instead of a command that knows how to
// undo itself (Commands/, original). Much simpler - no Undo() per action - but memory grows with document size x edits.
public class SnapshotEditorVariation
{
    private readonly Stack<string> undo = new();
    private readonly Stack<string> redo = new();

    public string Text { get; private set; } = "";

    public void Change(Func<string, string> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        string next = edit(Text); // if this throws, nothing is saved
        undo.Push(Text);
        redo.Clear();
        Text = next;
    }

    public bool Undo()
    {
        if (!undo.TryPop(out string? previous))
        {
            return false;
        }
        redo.Push(Text);
        Text = previous;
        return true;
    }

    public bool Redo()
    {
        if (!redo.TryPop(out string? next))
        {
            return false;
        }
        undo.Push(Text);
        Text = next;
        return true;
    }

    public int SavedCharacters => undo.Sum(text => text.Length) + redo.Sum(text => text.Length);
}
