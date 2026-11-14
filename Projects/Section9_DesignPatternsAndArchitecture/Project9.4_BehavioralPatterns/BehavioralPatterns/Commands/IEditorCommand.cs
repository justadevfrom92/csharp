namespace BehavioralPatterns.Commands;

// COMMAND: an ACTION turned into an OBJECT. Because it's an object it can be stored in a list,
// undone, redone, logged, queued or sent somewhere else to run later.
public interface IEditorCommand
{
    string Description { get; }

    void Execute();

    void Undo();
}
