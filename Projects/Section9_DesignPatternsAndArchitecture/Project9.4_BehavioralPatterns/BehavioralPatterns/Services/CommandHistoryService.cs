using BehavioralPatterns.Commands;

namespace BehavioralPatterns.Services;

// Runs commands and remembers them: two stacks give undo AND redo.
public class CommandHistoryService
{
    private readonly Stack<IEditorCommand> undoStack = new();
    private readonly Stack<IEditorCommand> redoStack = new();

    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    public void Execute(IEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Execute();          // if this throws, nothing is recorded
        undoStack.Push(command);
        redoStack.Clear();          // a NEW action makes the old "redo" future invalid
    }

    public bool Undo()
    {
        if (!undoStack.TryPop(out IEditorCommand? command))
        {
            return false;
        }
        command.Undo();
        redoStack.Push(command);
        return true;
    }

    public bool Redo()
    {
        if (!redoStack.TryPop(out IEditorCommand? command))
        {
            return false;
        }
        command.Execute();
        undoStack.Push(command);
        return true;
    }
}
