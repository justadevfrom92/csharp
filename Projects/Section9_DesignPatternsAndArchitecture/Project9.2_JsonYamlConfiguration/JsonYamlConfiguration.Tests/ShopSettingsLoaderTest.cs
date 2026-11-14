using JsonYamlConfiguration.Loaders;
using JsonYamlConfiguration.Settings;
using JsonYamlConfiguration.Tests.Helpers;

namespace JsonYamlConfiguration.Tests;

// PositivePositive = valid input -> works; PositiveNegative = well-formed input that breaks a rule -> refused;
// NegativeNegative = broken or missing input -> rejected.
public class ShopSettingsLoaderTest : IDisposable
{
    private readonly TempFolderHelper files = new TempFolderHelper();

    public void Dispose()
    {
        files.Dispose();
    }

    // ---------- JSON ----------

    [Fact]
    public void LoadJson_PositivePositive_ValidFile_BindsEverySection()
    {
        files.Write("shop.json", TempFolderHelper.ValidJson);

        ShopSettings settings = ShopSettingsLoader.Load(files.Folder, "Development");

        Assert.Equal("Test Shop", settings.Name);
        Assert.Equal(10, settings.MaximumItemsPerOrder);
        Assert.Equal(50m, settings.Shipping.FreeShippingFrom);
        Assert.Equal(["US", "CA", "GB"], settings.Shipping.Countries);
        Assert.Equal(10, settings.DiscountCodes["WELCOME10"]);
    }

    [Fact]
    public void LoadJson_PositiveNegative_ValidJsonBreakingRules_IsRefusedWithEveryReason()
    {
        files.Write("shop.json", """
            { "Shop": { "Name": "", "MaximumItemsPerOrder": 500, "Shipping": { "FreeShippingFrom": 50, "Countries": [] } } }
            """);

        var error = Assert.Throws<InvalidOperationException>(() => ShopSettingsLoader.Load(files.Folder, "Development"));

        Assert.Contains("Name", error.Message);
        Assert.Contains("MaximumItemsPerOrder", error.Message);
        Assert.Contains("at least one country", error.Message);
    }

    [Fact]
    public void LoadJson_NegativeNegative_MissingOrBrokenFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => ShopSettingsLoader.Load(files.Folder, "Development"));

        files.Write("shop.json", "{ \"Shop\": { \"Name\": ");
        Assert.Throws<InvalidDataException>(() => ShopSettingsLoader.Load(files.Folder, "Development"));
    }

    // ---------- YAML on top of JSON ----------

    [Fact]
    public void LoadYaml_PositivePositive_EnvironmentFile_OverridesOnlyTheValuesItHas()
    {
        files.Write("shop.json", TempFolderHelper.ValidJson);
        files.Write("shop.Production.yaml", """
            Shop:
              Name: Live Shop
              DiscountCodes:
                VIP25: 25
            """);

        ShopSettings production = ShopSettingsLoader.Load(files.Folder, "Production");
        ShopSettings development = ShopSettingsLoader.Load(files.Folder, "Development");

        Assert.Equal("Live Shop", production.Name);          // from YAML
        Assert.Equal(10, production.MaximumItemsPerOrder);    // still from JSON
        Assert.Equal(2, production.DiscountCodes.Count);      // dictionaries MERGE: WELCOME10 (JSON) + VIP25 (YAML)
        Assert.Equal("Test Shop", development.Name);          // no Development.yaml -> JSON only
    }

    [Fact]
    public void LoadYaml_PositiveNegative_ShorterListInYaml_DoesNotRemoveTheExtraJsonItems()
    {
        files.Write("shop.json", TempFolderHelper.ValidJson);
        files.Write("shop.Production.yaml", """
            Shop:
              Shipping:
                Countries:
                  - MX
            """);

        ShopSettings settings = ShopSettingsLoader.Load(files.Folder, "Production");

        // Lists are merged BY POSITION: item 0 is replaced, items 1 and 2 from JSON stay.
        Assert.Equal(["MX", "CA", "GB"], settings.Shipping.Countries);
    }

    [Fact]
    public void LoadYaml_NegativeNegative_BrokenYaml_Throws()
    {
        files.Write("shop.json", TempFolderHelper.ValidJson);
        files.Write("shop.Production.yaml", "Shop:\n  Name: [unclosed\n");

        Assert.Throws<InvalidDataException>(() => ShopSettingsLoader.Load(files.Folder, "Production"));
    }

    // ---------- overrides (like environment variables / command line) ----------

    [Fact]
    public void Override_PositivePositive_WinsOverJsonAndYaml()
    {
        files.Write("shop.json", TempFolderHelper.ValidJson);
        files.Write("shop.Production.yaml", "Shop:\n  Name: Live Shop\n");

        ShopSettings settings = ShopSettingsLoader.Load(files.Folder, "Production", [new("Shop:Name", "From override"), new("Shop:Shipping:FreeShippingFrom", "99.5")]);

        Assert.Equal("From override", settings.Name);
        Assert.Equal(99.5m, settings.Shipping.FreeShippingFrom); // text "99.5" converted to decimal
    }

    [Fact]
    public void Override_PositiveNegative_ValueBreakingARule_IsRefused()
    {
        files.Write("shop.json", TempFolderHelper.ValidJson);

        Assert.Throws<InvalidOperationException>(() => ShopSettingsLoader.Load(files.Folder, "Development", [new("Shop:MaximumItemsPerOrder", "0")]));
    }

    [Fact]
    public void Override_NegativeNegative_TextThatIsNotANumber_Throws()
    {
        files.Write("shop.json", TempFolderHelper.ValidJson);

        Assert.Throws<InvalidOperationException>(() => ShopSettingsLoader.Load(files.Folder, "Development", [new("Shop:MaximumItemsPerOrder", "lots")]));
    }

    [Theory]
    [InlineData("", "Development")]
    [InlineData("   ", "Development")]
    [InlineData("folder", "")]
    public void Load_NegativeNegative_MissingFolderOrEnvironment_Throws(string folder, string environment)
    {
        Assert.ThrowsAny<ArgumentException>(() => ShopSettingsLoader.Load(folder, environment));
    }
}
