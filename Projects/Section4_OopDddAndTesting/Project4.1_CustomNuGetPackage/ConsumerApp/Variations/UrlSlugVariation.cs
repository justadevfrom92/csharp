using System.Text.RegularExpressions;
using Roadmap.SimpleHelpers.Generators;

namespace ConsumerApp.Variations;

// The package's UrlSlugGenerator.MakeUrlSlug (a loop over characters), compared with other ways to write it.
public static class UrlSlugVariation
{
    // ORIGINAL (from the NuGet package): a static method on a static class.
    public static string FromPackage(string title)
    {
        return UrlSlugGenerator.MakeUrlSlug(title);
    }

    // Regex: two lines instead of a loop. "\p{L}" would also keep accented letters (é), unlike the original.
    public static string WithRegex(string title)
    {
        string dashed = Regex.Replace(title.ToLowerInvariant(), "[^a-z0-9]+", "-");
        return dashed.Trim('-');
    }

    // LINQ: replace every non-letter/digit with a space, then join the words with dashes.
    public static string WithLinq(string title)
    {
        string spaced = new string(title.ToLowerInvariant().Select(character => char.IsAsciiLetterOrDigit(character) ? character : ' ').ToArray());
        return string.Join('-', spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    // EXTENSION METHOD: same code as the package, but called like  "Hello World".ToUrlSlug().
    // Static class + "this" on the first parameter. The package could offer this style too.
    public static string ToUrlSlug(this string title)
    {
        return UrlSlugGenerator.MakeUrlSlug(title);
    }
}
