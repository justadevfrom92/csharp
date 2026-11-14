using PipelinePatterns.Filters;

namespace PipelinePatterns.Pipelines;

// Runs the filters in the order they were added. Add() returns "this" so a pipeline reads top to bottom.
public class TextPipeline
{
    private readonly List<ITextFilter> filters = new();

    public int FilterCount => filters.Count;

    public TextPipeline Add(ITextFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        filters.Add(filter);
        return this;
    }

    public string Run(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string current = text;
        foreach (ITextFilter filter in filters)
        {
            current = filter.Apply(current); // the "pipe": one filter's output is the next one's input
        }
        return current;
    }
}
