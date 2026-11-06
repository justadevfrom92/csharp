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
├── .Text        UrlSlugMaker, TextShortener
└── .Validation  PaymentCardNumberChecker
```
The root matches the package name, so users can guess the `using` from the package they
installed. Classes are grouped by what they work on (text vs validation), so a user only
imports what they need, and a new helper has an obvious home.

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

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `static class` for every helper | They keep no state: same input, same output. Just call `UrlSlugMaker.MakeUrlSlug(...)`. | An instance class would force users to `new` an object that holds nothing. |
| No interfaces | Pure functions don't need swapping out in tests — you test them directly. | `IUrlSlugMaker` would double the public API for no benefit. |
| `StringBuilder` in `MakeUrlSlug` | Builds the slug one character at a time. | `string +=` creates a new string per character. |
| `char.IsAsciiLetterOrDigit` | Slugs must be plain a–z/0–9 to be safe in every URL. | `char.IsLetterOrDigit` also accepts `é`, `ß`, Chinese characters. |
| `bool IsValidCardNumber` | The caller just wants yes/no; a bad card number is a normal answer. | Throwing would force try/catch around a simple check. |
| `ArgumentOutOfRangeException` in `ShortenWithEllipsis` | `maximumLength < 4` is a *programmer* mistake, not user input. Fail loudly. | Returning something odd would hide the bug. |
| `int digit = character - '0'` | Characters are numbers underneath; `'7' - '0'` = 7, no parsing or allocation. | `int.Parse(character.ToString())` creates a string per digit. |
