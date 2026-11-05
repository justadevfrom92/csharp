namespace Roadmap.SimpleHelpers.Text;

/// <summary>Shortens long text for previews.</summary>
public static class TextShortener
{
    /// <summary>Cuts the text to <paramref name="maximumLength"/> characters and adds "..." if it was cut.</summary>
    public static string ShortenWithEllipsis(string text, int maximumLength)
    {
        if (maximumLength < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLength), "Must be at least 4.");
        }
        if (text.Length <= maximumLength)
        {
            return text;
        }
        return text.Substring(0, maximumLength - 3) + "...";
    }
}
