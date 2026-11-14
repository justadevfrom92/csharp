using System.Text.RegularExpressions;

namespace PipelinePatterns.Filters;

// "4111 1111 1111 1234" -> "**** 1234"
public class MaskCardNumberFilter : ITextFilter
{
    public string Apply(string text)
    {
        return Regex.Replace(text, @"\b(?:\d[ -]?){12}(\d{4})\b", "**** $1");
    }
}
