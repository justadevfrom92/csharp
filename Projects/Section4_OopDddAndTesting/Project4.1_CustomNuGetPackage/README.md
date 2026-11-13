# Project 4.1 — Custom NuGet Package

- `SimpleHelpers/` — a class library, packed as `Roadmap.SimpleHelpers`.
- `ConsumerApp/` — a console app that installs that package from a local folder.
- `LocalNuGetFeed/` — where `dotnet pack` puts the `.nupkg` (ignored by git).

## Run

```bash
dotnet pack SimpleHelpers -c Release
```

```bash
dotnet run --project ConsumerApp
```

`ConsumerApp/nuget.config` tells NuGet to look in `../LocalNuGetFeed`.
If you change the library, bump `<Version>` in `SimpleHelpers.csproj` and in
`ConsumerApp.csproj` — NuGet caches packages by version, so the same number won't update.

Add a 3rd-party package the normal way, e.g. `dotnet add package Humanizer.Core`.

## Why these choices

**Namespaces.**
```
Roadmap.SimpleHelpers              (package name = root namespace)
├── .Generators  UrlSlugGenerator
├── .Shorteners  TextShortener
└── .Validators  PaymentCardNumberValidator
```
The root matches the package name, so users can guess the `using` from the package they
installed. Each folder (and namespace) is named after the KIND of class in it, and every class
name ends in that kind: a `...Generator` is in `Generators/`, a `...Validator` in `Validators/`.
So the name alone tells you where to find a class, and a new helper has an obvious home.

**Version 2.0.0:** the class names changed (`UrlSlugMaker` → `UrlSlugGenerator`,
`PaymentCardNumberChecker` → `PaymentCardNumberValidator`), which breaks code using 1.0.0.
Breaking changes bump the FIRST number of the version, so users can see it's not a drop-in upgrade.

**Public API: small and static.**
- Each helper is a `static class` with one clear method. They don't keep any state, so there
  is nothing to create or configure — just call it.
- Method names say exactly what they do (`IsValidCardNumber`, not `Check`).
- Only what users need is `public`. Anything public is a promise: changing it later breaks
  other people's code. Fewer public things = fewer promises.
- `GenerateDocumentationFile` ships the `///` comments, so users see help text in their editor.

**Why a package and not a project reference.** A project reference only works inside one
repo. A package has a version number, so many apps can use it and upgrade when they choose.

**Why `ConsumerApp` is not in the solution file.** It can't restore until the package has been
packed. Leaving it out keeps `dotnet build` of the whole solution working on a fresh clone.

## Code snippets

**Package info in the library's `.csproj`**, from `SimpleHelpers/SimpleHelpers.csproj`

```xml
<PackageId>Roadmap.SimpleHelpers</PackageId>
<Version>2.0.0</Version>
<Description>Small text and validation helpers.</Description>
<GenerateDocumentationFile>true</GenerateDocumentationFile> <!-- /// comments show up in the user's editor -->
<PackageOutputPath>../LocalNuGetFeed</PackageOutputPath>
```

**Pack it into a `.nupkg`**

```bash
dotnet pack SimpleHelpers -c Release
```

**Tell the consumer where to find it**, from `ConsumerApp/nuget.config`

```xml
<packageSources>
  <add key="LocalFeed" value="../LocalNuGetFeed" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
```

**Use it as a package, not a project**, from `ConsumerApp/ConsumerApp.csproj`

```xml
<PackageReference Include="Roadmap.SimpleHelpers" Version="2.0.0" />
```

**A static helper with XML doc comments**, from `SimpleHelpers/Generators/UrlSlugGenerator.cs`

```csharp
/// <summary>Turns titles into URL-friendly text.</summary>
public static class UrlSlugGenerator
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
```

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `static class` for every helper | They keep no state: same input, same output. Just call `UrlSlugGenerator.MakeUrlSlug(...)`. | An instance class would force users to `new` an object that holds nothing. |
| No interfaces | Pure functions don't need swapping out in tests — you test them directly. | `IUrlSlugGenerator` would double the public API for no benefit. |
| `StringBuilder` in `MakeUrlSlug` | Builds the slug one character at a time. | `string +=` creates a new string per character. |
| `char.IsAsciiLetterOrDigit` | Slugs must be plain a–z/0–9 to be safe in every URL. | `char.IsLetterOrDigit` also accepts `é`, `ß`, Chinese characters. |
| `bool IsValidCardNumber` | The caller just wants yes/no; a bad card number is a normal answer. | Throwing would force try/catch around a simple check. |
| `ArgumentOutOfRangeException` in `ShortenWithEllipsis` | `maximumLength < 4` is a *programmer* mistake, not user input. Fail loudly. | Returning something odd would hide the bug. |
| `int digit = character - '0'` | Characters are numbers underneath; `'7' - '0'` = 7, no parsing or allocation. | `int.Parse(character.ToString())` creates a string per digit. |
