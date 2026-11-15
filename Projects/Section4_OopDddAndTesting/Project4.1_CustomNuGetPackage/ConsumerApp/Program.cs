using Roadmap.SimpleHelpers.Generators;
using Roadmap.SimpleHelpers.Shorteners;
using Roadmap.SimpleHelpers.Validators;

Console.WriteLine("Using the Roadmap.SimpleHelpers NuGet package:");
Console.WriteLine();

Console.WriteLine(UrlSlugGenerator.MakeUrlSlug("Hello, World! C# in 2026"));
Console.WriteLine(TextShortener.ShortenWithEllipsis("This sentence is far too long for a preview box.", 20));

string[] cardNumbers = ["4111 1111 1111 1111", "4111 1111 1111 1112", "abcd"];
foreach (string cardNumber in cardNumbers)
{
    Console.WriteLine($"{cardNumber,-22} valid? {PaymentCardNumberValidator.IsValidCardNumber(cardNumber)}");
}

// ---------- Variations (ConsumerApp/Variations/UrlSlugVariation.cs): same slug, different code ----------
Console.WriteLine();
foreach (string title in new[] { "Hello, World! C# in 2026", "  --Café au lait--  ", "" })
{
    Console.WriteLine($"'{title}' -> package '{ConsumerApp.Variations.UrlSlugVariation.FromPackage(title)}', regex '{ConsumerApp.Variations.UrlSlugVariation.WithRegex(title)}', linq '{ConsumerApp.Variations.UrlSlugVariation.WithLinq(title)}', extension '{ConsumerApp.Variations.UrlSlugVariation.ToUrlSlug(title)}'");
}
