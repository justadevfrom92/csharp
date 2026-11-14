using System.Text.RegularExpressions;

namespace PipelinePatterns.Filters;

// "  a   b  " -> "a b"
public class TrimWhitespaceFilter : ITextFilter
{
    public string Apply(string text)
    {
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
