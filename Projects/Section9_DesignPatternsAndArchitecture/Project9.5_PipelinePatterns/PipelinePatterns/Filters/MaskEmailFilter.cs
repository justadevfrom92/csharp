using System.Text.RegularExpressions;

namespace PipelinePatterns.Filters;

// "ann@example.com" -> "[email]"  (don't keep personal data in logs)
public class MaskEmailFilter : ITextFilter
{
    public string Apply(string text)
    {
        return Regex.Replace(text, @"[\w.+-]+@[\w-]+\.[\w.]+", "[email]");
    }
}
