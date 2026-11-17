using System.Text.Json;
using JsonYamlConfiguration.Settings;
using JsonYamlConfiguration.Tests.Helpers;
using JsonYamlConfiguration.Variations;
using Microsoft.Extensions.Configuration;

namespace JsonYamlConfiguration.Tests;

// Other formats and other ways to read settings (JsonYamlConfiguration/Variations/ConfigReadingVariation.cs).
public class VariationsTest : IDisposable
{
    private readonly TempFolderHelper files = new TempFolderHelper();

    public void Dispose()
    {
        files.Dispose();
    }

    [Fact]
    public void Formats_PositivePositive_IniXmlAndCommandLine_GiveTheSameKey()
    {
        files.Write("shop.ini", "[Shop]\nName=Test Shop\n");
        files.Write("shop.xml", "<settings><Shop><Name>Test Shop</Name></Shop></settings>");

        Assert.Equal("Test Shop", ConfigReadingVariation.NameFromIni(files.Folder, "shop.ini"));
        Assert.Equal("Test Shop", ConfigReadingVariation.NameFromXml(files.Folder, "shop.xml"));
        Assert.Equal("Test Shop", ConfigReadingVariation.NameFromCommandLine(["--Shop:Name", "Test Shop"]));
    }

    [Fact]
    public void Reading_PositiveNegative_BindKeepsMissingValues_GetValueUsesTheDefault()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection([new("Shop:Name", "Test Shop")]).Build();

        ShopSettings bound = ConfigReadingVariation.BindIntoExisting(configuration);

        Assert.Equal("Test Shop", bound.Name);
        Assert.Equal(99, bound.MaximumItemsPerOrder);                                // not in the config: kept, not reset to 0
        Assert.Equal(10, ConfigReadingVariation.MaximumItemsOrDefault(configuration)); // missing key -> the default
    }

    [Fact]
    public void Json_PositiveNegative_PlainSystemTextJson_RejectsTheCommentsTheConfigReaderAccepts()
    {
        files.Write("shop.json", "{\n  // a comment, like the real shop.json\n  \"Shop\": { \"Name\": \"Test Shop\", \"MaximumItemsPerOrder\": 5, },\n}");
        string path = Path.Combine(files.Folder, "shop.json");

        Assert.ThrowsAny<JsonException>(() => ConfigReadingVariation.DeserializeJsonFile(path, allowComments: false));
        Assert.Equal("Test Shop", ConfigReadingVariation.DeserializeJsonFile(path, allowComments: true)!.Name);
        Assert.Equal("Test Shop", new ConfigurationBuilder().AddJsonFile(path).Build()["Shop:Name"]); // the config reader just works
    }

    [Fact]
    public void Formats_NegativeNegative_BrokenIniOrXml_Throw()
    {
        files.Write("bad.ini", "[Shop\nName=Test");
        files.Write("bad.xml", "<settings><Shop>");

        Assert.Throws<InvalidDataException>(() => ConfigReadingVariation.NameFromIni(files.Folder, "bad.ini"));
        Assert.Throws<InvalidDataException>(() => ConfigReadingVariation.NameFromXml(files.Folder, "bad.xml"));
    }
}
