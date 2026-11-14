namespace PipelinePatterns.Filters;

// PIPES AND FILTERS: a job split into small independent steps ("filters"). Each one takes the output
// of the step before ("the pipe") and passes its own output on. Filters don't know about each other,
// so they can be added, removed, re-ordered and tested one at a time.
public interface ITextFilter
{
    string Apply(string text);
}
