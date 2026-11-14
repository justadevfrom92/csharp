using System.ComponentModel.DataAnnotations;
using JsonYamlConfiguration.Settings;
using Microsoft.Extensions.Configuration;

namespace JsonYamlConfiguration.Loaders;

// Builds configuration from several SOURCES, in order. A LATER source wins for the same key:
//   1. shop.json                       (base values, required)
//   2. shop.<environment>.yaml         (optional, per environment)
//   3. environment variables SHOP_...  (e.g. SHOP_Shop__Name="Test"; "__" = ":")
//   4. overrides                       (in code / tests; like command-line arguments)
// Then binds the "Shop" section to ShopSettings and checks the rules, so a bad file stops the app at startup.
public static class ShopSettingsLoader
{
    public static ShopSettings Load(string folder, string environment, IEnumerable<KeyValuePair<string, string?>>? overrides = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(folder)
            .AddJsonFile("shop.json", optional: false)
            .AddYamlFile($"shop.{environment}.yaml", optional: true)
            .AddEnvironmentVariables(prefix: "SHOP_")
            .AddInMemoryCollection(overrides ?? [])
            .Build();

        ShopSettings settings = configuration.GetSection("Shop").Get<ShopSettings>()
            ?? throw new InvalidOperationException("The 'Shop' section is missing.");

        List<string> errors = Validate(settings);
        errors.AddRange(Validate(settings.Shipping));
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Invalid shop settings: " + string.Join(" ", errors));
        }
        return settings;
    }

    // Reads one value by its full PATH, without binding a class.
    public static string? ReadValue(string folder, string environment, string path)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(folder)
            .AddJsonFile("shop.json", optional: false)
            .AddYamlFile($"shop.{environment}.yaml", optional: true)
            .Build();
        return configuration[path];
    }

    private static List<string> Validate(object settings)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), results, validateAllProperties: true);
        return results.Select(result => result.ErrorMessage ?? "Invalid value.").ToList();
    }
}
