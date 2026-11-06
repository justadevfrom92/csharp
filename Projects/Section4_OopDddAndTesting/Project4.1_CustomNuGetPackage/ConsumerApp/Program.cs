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
