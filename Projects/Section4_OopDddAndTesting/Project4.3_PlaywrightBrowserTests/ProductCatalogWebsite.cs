using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;

namespace PlaywrightBrowserTests;

// Starts the Product Catalog site on a real port + one headless browser.
// xUnit creates ONE of these for the test class (IClassFixture) and disposes it at the end.
public class ProductCatalogWebsite : IAsyncLifetime
{
    private WebApplicationFactory<Program>? factory;
    private IPlaywright? playwright;

    public string BaseUrl { get; private set; } = "";
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        factory = new WebApplicationFactory<Program>();
        factory.UseKestrel(0); // real HTTP server on a free port, so a real browser can reach it
        factory.StartServer();
        BaseUrl = factory.ClientOptions.BaseAddress.ToString().TrimEnd('/');

        playwright = await Playwright.CreateAsync();
        var launchOptions = new BrowserTypeLaunchOptions { Headless = true };

        // Optional: use an already-installed Chromium instead of the one Playwright downloads.
        string? chromiumPath = Environment.GetEnvironmentVariable("CHROMIUM_PATH");
        if (!string.IsNullOrEmpty(chromiumPath))
        {
            launchOptions.ExecutablePath = chromiumPath;
        }
        Browser = await playwright.Chromium.LaunchAsync(launchOptions);
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        playwright?.Dispose();
        if (factory != null)
        {
            await factory.DisposeAsync();
        }
    }
}
