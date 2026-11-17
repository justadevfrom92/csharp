using PipelinePatterns.Filters;

namespace PipelinePatterns.Variations;

// Pipes and filters without TextPipeline (original): the filters as a list of functions, run with LINQ Aggregate.
// Aggregate = "start with a value, feed it through every item in order" - exactly a pipeline.
public static class TextPipelineVariation
{
    public static string WithAggregate(string text, IEnumerable<Func<string, string>> filters)
    {
        ArgumentNullException.ThrowIfNull(text);
        return filters.Aggregate(text, (current, filter) => filter(current));
    }

    // The original's filter OBJECTS can be used the same way via method groups (filter.Apply).
    public static string WithFilterObjects(string text, IEnumerable<ITextFilter> filters)
    {
        return WithAggregate(text, filters.Select(filter => (Func<string, string>)filter.Apply));
    }
}
