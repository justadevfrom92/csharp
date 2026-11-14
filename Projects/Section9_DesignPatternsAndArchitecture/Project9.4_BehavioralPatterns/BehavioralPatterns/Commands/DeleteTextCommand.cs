using BehavioralPatterns.Models;

namespace BehavioralPatterns.Commands;

public class DeleteTextCommand : IEditorCommand
{
    private readonly TextDocumentModel document;
    private readonly int position;
    private readonly int length;
    private string deletedText = "";   // remembered in Execute, so Undo can put it back

    public DeleteTextCommand(TextDocumentModel document, int position, int length)
    {
        ArgumentNullException.ThrowIfNull(document);
        this.document = document;
        this.position = position;
        this.length = length;
    }

    public string Description => $"delete {length} at {position}";

    public void Execute()
    {
        if (length <= 0 || position < 0 || position + length > document.Text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Nothing to delete at that position.");
        }
        deletedText = document.Text.ToString(position, length);
        document.Text.Remove(position, length);
    }

    public void Undo()
    {
        document.Text.Insert(position, deletedText);
    }
}
