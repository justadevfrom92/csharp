namespace JsonYamlConfiguration.Tests.Helpers;

// A brand-new empty folder per test, deleted afterwards, so tests never see each other's files.
public class TempFolderHelper : IDisposable
{
    public const string ValidJson = """
        {
          "Shop": {
            "Name": "Test Shop",
            "MaximumItemsPerOrder": 10,
            "Shipping": { "FreeShippingFrom": 50, "Countries": [ "US", "CA", "GB" ] },
            "DiscountCodes": { "WELCOME10": 10 }
          }
        }
        """;

    public TempFolderHelper()
    {
        Folder = Path.Combine(Path.GetTempPath(), "config-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Folder);
    }

    public string Folder { get; }

    public void Write(string fileName, string content)
    {
        File.WriteAllText(Path.Combine(Folder, fileName), content);
    }

    public void Dispose()
    {
        Directory.Delete(Folder, recursive: true);
    }
}
