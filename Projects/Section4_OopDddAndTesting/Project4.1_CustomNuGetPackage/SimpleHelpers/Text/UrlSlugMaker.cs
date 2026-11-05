using System.Text;

namespace Roadmap.SimpleHelpers.Text;

/// <summary>Turns titles into URL-friendly text.</summary>
public static class UrlSlugMaker
{
    /// <summary>"Hello, World! 2026" becomes "hello-world-2026".</summary>
    public static string MakeUrlSlug(string title)
    {
        var slug = new StringBuilder();
        bool lastWasDash = false;

        foreach (char character in title.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                slug.Append(character);
                lastWasDash = false;
            }
            else if (!lastWasDash && slug.Length > 0)
            {
                slug.Append('-');
                lastWasDash = true;
            }
        }

        return slug.ToString().TrimEnd('-');
    }
}
