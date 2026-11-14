using BehavioralPatterns.Models;

namespace BehavioralPatterns.Commands;

public class InsertTextCommand : IEditorCommand
{
    private readonly TextDocumentModel document;
    private readonly int position;
    private readonly string text;

    public InsertTextCommand(TextDocumentModel document, int position, string text)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrEmpty(text);
        this.document = document;
        this.position = position;
        this.text = text;
    }

    public string Description => $"insert '{text}' at {position}";

    public void Execute()
    {
        if (position < 0 || position > document.Text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position), $"Position must be 0 to {document.Text.Length}.");
        }
        document.Text.Insert(position, text);
    }

    public void Undo()
    {
        document.Text.Remove(position, text.Length);
    }
}
