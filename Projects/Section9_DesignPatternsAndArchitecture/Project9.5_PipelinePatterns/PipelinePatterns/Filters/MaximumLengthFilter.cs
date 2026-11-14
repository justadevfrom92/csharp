namespace PipelinePatterns.Filters;

// Cuts long text and adds "..." so one huge message can't flood the log.
public class MaximumLengthFilter : ITextFilter
{
    private readonly int maximumLength;

    public MaximumLengthFilter(int maximumLength)
    {
        if (maximumLength < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLength), "Must be at least 4.");
        }
        this.maximumLength = maximumLength;
    }

    public string Apply(string text)
    {
        return text.Length <= maximumLength ? text : text[..(maximumLength - 3)] + "...";
    }
}
