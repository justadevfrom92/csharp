using System.Globalization;
using System.Text;

namespace Roadmap.PreciseUtilities.Text;

/// <summary>Builds lowercase, ASCII-only, hyphen-separated URL path segments ("slugs").</summary>
public static class UrlSlugGenerator
{
    /// <summary>Default maximum slug length; long enough for titles, short enough for readable URLs.</summary>
    public const int DefaultMaximumSlugLength = 80;

    /// <summary>
    /// Converts free text into a URL-safe slug: accents are stripped ("é" → "e"), every run of
    /// non-alphanumeric characters becomes a single hyphen, and leading/trailing hyphens are removed.
    /// </summary>
    /// <param name="sourceText">Any human-readable text, e.g. a product or article title.</param>
    /// <param name="maximumSlugLength">Upper bound; the slug is cut at a hyphen boundary when possible.</param>
    /// <returns>The slug, or an empty string when the text contains no letters or digits.</returns>
    public static string GenerateUrlSafeSlug(string sourceText, int maximumSlugLength = DefaultMaximumSlugLength)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        Guards.ArgumentRangeGuard.ThrowIfOutsideInclusiveRange(maximumSlugLength, 1, 2048);

        string decomposedText = sourceText.Normalize(NormalizationForm.FormD);
        var slugBuilder = new StringBuilder(decomposedText.Length);
        bool previousCharacterWasSeparator = true; // suppresses a leading hyphen

        foreach (char character in decomposedText)
        {
            UnicodeCategory characterCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (characterCategory == UnicodeCategory.NonSpacingMark)
            {
                continue; // the accent part of a decomposed "é"
            }
            if (char.IsAsciiLetterOrDigit(character))
            {
                slugBuilder.Append(char.ToLowerInvariant(character));
                previousCharacterWasSeparator = false;
            }
            else if (!previousCharacterWasSeparator)
            {
                slugBuilder.Append('-');
                previousCharacterWasSeparator = true;
            }
        }

        string slug = slugBuilder.ToString().TrimEnd('-');
        if (slug.Length <= maximumSlugLength)
        {
            return slug;
        }

        string truncatedSlug = slug[..maximumSlugLength];
        int lastHyphenIndex = truncatedSlug.LastIndexOf('-');
        return (lastHyphenIndex > 0 ? truncatedSlug[..lastHyphenIndex] : truncatedSlug).TrimEnd('-');
    }
}
