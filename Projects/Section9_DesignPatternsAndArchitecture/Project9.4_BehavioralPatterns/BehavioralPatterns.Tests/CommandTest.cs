using BehavioralPatterns.Commands;
using BehavioralPatterns.Models;
using BehavioralPatterns.Services;

namespace BehavioralPatterns.Tests;

public class CommandTest
{
    private readonly TextDocumentModel document = new TextDocumentModel();
    private readonly CommandHistoryService history = new CommandHistoryService();

    [Fact]
    public void UndoRedo_PositivePositive_EditsCanBeUndoneAndRedoneInOrder()
    {
        history.Execute(new InsertTextCommand(document, 0, "Hello"));
        history.Execute(new InsertTextCommand(document, 5, " world"));
        history.Execute(new DeleteTextCommand(document, 0, 6));
        Assert.Equal("world", document.ToString());

        history.Undo();
        Assert.Equal("Hello world", document.ToString());
        history.Undo();
        Assert.Equal("Hello", document.ToString());
        history.Redo();
        Assert.Equal("Hello world", document.ToString());
    }

    [Fact]
    public void UndoRedo_PositiveNegative_NothingToUndo_ReturnsFalse_AndNewEditClearsRedo()
    {
        Assert.False(history.Undo());
        Assert.False(history.Redo());

        history.Execute(new InsertTextCommand(document, 0, "A"));
        history.Undo();
        history.Execute(new InsertTextCommand(document, 0, "B")); // a new action after undo...

        Assert.False(history.CanRedo);                            // ...removes the old redo
        Assert.Equal("B", document.ToString());
    }

    [Fact]
    public void Execute_NegativeNegative_InvalidPositionOrNull_ThrowsAndRecordsNothing()
    {
        history.Execute(new InsertTextCommand(document, 0, "Hi"));

        Assert.Throws<ArgumentOutOfRangeException>(() => history.Execute(new InsertTextCommand(document, 99, "x")));
        Assert.Throws<ArgumentOutOfRangeException>(() => history.Execute(new DeleteTextCommand(document, 1, 5)));
        Assert.Throws<ArgumentNullException>(() => history.Execute(null!));
        Assert.ThrowsAny<ArgumentException>(() => new InsertTextCommand(document, 0, ""));

        history.Undo();
        Assert.Equal("", document.ToString()); // only the one valid edit was recorded
        Assert.False(history.CanUndo);
    }
}
