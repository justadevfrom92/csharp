using System.Text.Json;
using JsonYamlConfiguration.Settings;
using Microsoft.Extensions.Configuration;

namespace JsonYamlConfiguration.Variations;

// The shop name read from other FILE FORMATS, and settings read other WAYS. ShopSettingsLoader (original) uses
// JSON + YAML + environment variables + Get<ShopSettings>().
public static class ConfigReadingVariation
{
    // ---------- other formats: all produce the same key "Shop:Name" ----------

    // INI:  [Shop]  then  Name=...
    public static string? NameFromIni(string folder, string fileName)
    {
        return new ConfigurationBuilder().SetBasePath(folder).AddIniFile(fileName).Build()["Shop:Name"];
    }

    // XML:  <settings><Shop><Name>...</Name></Shop></settings>  (the root element's name is ignored)
    public static string? NameFromXml(string folder, string fileName)
    {
        return new ConfigurationBuilder().SetBasePath(folder).AddXmlFile(fileName).Build()["Shop:Name"];
    }

    // Command line:  --Shop:Name "Test Shop"   (what "dotnet run -- --Shop:Name X" passes to a .NET app)
    public static string? NameFromCommandLine(string[] args)
    {
        return new ConfigurationBuilder().AddCommandLine(args).Build()["Shop:Name"];
    }

    // ---------- other ways to read ----------

    // Bind into an object you already have (instead of Get<T>() creating a new one).
    public static ShopSettings BindIntoExisting(IConfiguration configuration)
    {
        var settings = new ShopSettings { MaximumItemsPerOrder = 99 }; // values not in the config stay as they are
        configuration.GetSection("Shop").Bind(settings);
        return settings;
    }

    // GetValue<T> for ONE value, with a default when the key is missing (no exception, no class).
    public static int MaximumItemsOrDefault(IConfiguration configuration)
    {
        return configuration.GetValue("Shop:MaximumItemsPerOrder", defaultValue: 10);
    }

    // System.Text.Json straight from the file, no configuration system: no layering, no environment overrides,
    // and by DEFAULT it REJECTS comments - shop.json has "// ..." comments, so this throws JsonException.
    public static ShopSettings? DeserializeJsonFile(string path, bool allowComments)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = allowComments ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow,
            AllowTrailingCommas = allowComments,
        };
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
        {
            CommentHandling = options.ReadCommentHandling,
            AllowTrailingCommas = options.AllowTrailingCommas,
        });
        return document.RootElement.GetProperty("Shop").Deserialize<ShopSettings>(options);
    }
}
