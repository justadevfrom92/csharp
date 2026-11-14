using System.Text;

namespace BehavioralPatterns.Models;

// The thing the commands change (the "receiver").
public class TextDocumentModel
{
    public StringBuilder Text { get; } = new StringBuilder();

    public override string ToString()
    {
        return Text.ToString();
    }
}
