# Project 9.2 — JSON & YAML Configuration

Settings read from a JSON file, a YAML file per environment, environment variables and in-code overrides,
**layered** on top of each other, bound to C# classes and **validated** at startup.

```bash
cd JsonYamlConfiguration
dotnet run
```

```bash
cd JsonYamlConfiguration.Tests
dotnet test
```

## The layers (later wins)

```
shop.json                     base values (required)
   ▲ overridden by
shop.<Environment>.yaml       only the values that differ in that environment (optional)
   ▲ overridden by
SHOP_ environment variables   SHOP_Shop__Name="Test"   ("__" means ":")
   ▲ overridden by
in-code overrides             like command-line arguments; used by the tests
```

ASP.NET Core does exactly this with `appsettings.json` → `appsettings.{Environment}.json` → user secrets →
environment variables → command line (see 6.3, 7.3, 7.5). This project builds it by hand with
`ConfigurationBuilder`, so you can see every step, and adds YAML.

## JSON vs YAML

| | JSON (`shop.json`) | YAML (`shop.Production.yaml`) |
|---|---|---|
| Nesting | `{ }` | indentation (spaces, **never tabs**) |
| Lists | `[ "US", "CA" ]` | one `- US` per line |
| Comments | `//` (allowed by .NET's reader) | `#` |
| Built into .NET | yes | no — package `NetEscapades.Configuration.Yaml` adds `AddYamlFile(...)` |
| Common for | .NET apps, APIs | Kubernetes, Docker Compose, GitHub Actions (7.6), many DevOps tools |

Both become the **same thing** inside .NET: flat keys with `:` between the levels —
`Shop:Shipping:Countries:0 = US`. That's why a YAML file can override a JSON file.

## Merging rules (the surprising part)

- **Plain values:** the later file replaces it.
- **Objects / dictionaries:** **merged** key by key. YAML changes `VIP25`; `WELCOME10` still comes from JSON.
- **Lists: merged BY POSITION.** JSON has `[US, CA, GB]`, YAML has `[US, CA]` → the result is still
  `[US, CA, GB]`, because YAML only replaced items 0 and 1. **A later file can't make a list shorter.**
  If an environment needs a shorter list, don't put the list in the base file (or use a separate key).
  There's a test for this.

## Why validate at startup

A typo in a config file (`MaximumItemsPerOrder: 500`, an empty `Name`) should stop the app **immediately**
with a clear message, not cause odd behaviour hours later. `ShopSettingsLoader` runs the `[Required]`,
`[Range]`, `[MinLength]` rules and throws one error listing every problem. (In ASP.NET Core:
`.ValidateDataAnnotations().ValidateOnStart()`, see 6.3.)

## Code snippets

**Building configuration from several sources**, from `Loaders/ShopSettingsLoader.cs`

```csharp
IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(folder)
    .AddJsonFile("shop.json", optional: false)
    .AddYamlFile($"shop.{environment}.yaml", optional: true)
    .AddEnvironmentVariables(prefix: "SHOP_")
    .AddInMemoryCollection(overrides ?? [])
    .Build();
```

**Binding a section to a class, then validating it**

```csharp
ShopSettings settings = configuration.GetSection("Shop").Get<ShopSettings>()
    ?? throw new InvalidOperationException("The 'Shop' section is missing.");

var results = new List<ValidationResult>();
Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true);
```

**Reading one value by its path**

```csharp
string? firstCountry = configuration["Shop:Shipping:Countries:0"];
```

**The settings classes**, from `Settings/`

```csharp
public class ShopSettings
{
    [Required]
    [StringLength(60)]
    public string Name { get; set; } = "";

    [Range(1, 100)]
    public int MaximumItemsPerOrder { get; set; }

    public ShippingSettings Shipping { get; set; } = new();          // nested object

    public Dictionary<string, int> DiscountCodes { get; set; } = new(); // free key names
}
```

**The same section in JSON and YAML**

```json
"Shop": {
  "Shipping": {
    "FreeShippingFrom": 50.0,
    "Countries": [ "US", "CA", "GB" ]
  }
}
```

```yaml
Shop:
  Shipping:
    FreeShippingFrom: 75.0
    Countries:
      - US
      - CA
```

**Copy the files next to the program**, from `JsonYamlConfiguration.csproj`

```xml
<None Update="shop.json" CopyToOutputDirectory="PreserveNewest" />
<None Update="shop.Production.yaml" CopyToOutputDirectory="PreserveNewest" />
```

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| Settings are `class` with `{ get; set; }` | The binder creates the object empty, then sets properties one by one. | A `record` with a constructor binds too, but the validation attributes and defaults are clearer this way. |
| `List<string> Countries` | A YAML/JSON list. | `string[]` works the same; `List` matches the rest of the repo. |
| `Dictionary<string, int> DiscountCodes` | Key names aren't known in advance (any code). | A class would need a property per code. |
| `decimal FreeShippingFrom` | Money. | `double` (see [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md)). |
| `static class ShopSettingsLoader` | No state; one job. | — |
| `IEnumerable<KeyValuePair<string, string?>>? overrides` | Exactly what `AddInMemoryCollection` takes; optional. | — |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| JSON | every section binds | valid JSON breaking 3 rules → one error listing all 3 | missing file → `FileNotFoundException`; broken JSON → `InvalidDataException` |
| YAML over JSON | only the values in YAML change; dictionaries merge | shorter YAML list **doesn't** remove the extra JSON items | broken YAML → `InvalidDataException` |
| overrides | win over JSON and YAML; text converted to `decimal` | value breaking a rule → refused | `"lots"` for a number → `InvalidOperationException`; empty folder/environment → `ArgumentException` |
