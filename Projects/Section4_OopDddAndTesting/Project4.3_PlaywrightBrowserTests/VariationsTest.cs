using Microsoft.Playwright;
using PlaywrightBrowserTests.Fixtures;

namespace PlaywrightBrowserTests;

// The same browser actions written different ways. ProductCatalogBrowserTest (the original) uses CSS ids
// ("#Name", "#save-button") and reads text then Assert.Equal. Here: labels, roles, XPath, and Playwright's
// own Expect(...) assertions, which WAIT and RETRY until the page matches (or a timeout passes).
//   PositivePositive = valid input -> the same result every way
//   PositiveNegative = valid input where the ways BEHAVE differently (waiting, failing)
//   NegativeNegative = a broken selector -> rejected
public class VariationsTest : IClassFixture<ProductCatalogWebsiteFixture>, IAsyncLifetime
{
    private readonly ProductCatalogWebsiteFixture website;
    private IBrowserContext context = null!;
    private IPage page = null!;

    public VariationsTest(ProductCatalogWebsiteFixture website)
    {
        this.website = website;
    }

    public async Task InitializeAsync()
    {
        context = await website.Browser.NewContextAsync();
        page = await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
    }

    [Fact]
    public async Task Selectors_PositivePositive_LabelRoleAndXPath_FillTheSameFormAsCssIds()
    {
        string productName = "Lamp " + Guid.NewGuid().ToString("N")[..6];
        await page.GotoAsync(website.BaseUrl + "/Products/Create");

        await page.GetByLabel("Name").FillAsync(productName);                           // by the <label> text, like a user reads it
        await page.GetByLabel("Category").SelectOptionAsync("Lighting");
        await page.Locator("xpath=//input[@name='Price']").FillAsync("19.99");          // XPath: powerful, hard to read
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();    // by ROLE + visible name

        await Assertions.Expect(page).ToHaveURLAsync(website.BaseUrl + "/");            // waits for the redirect
        await Assertions.Expect(page.Locator("#success-alert")).ToContainTextAsync(productName);
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = productName })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Waiting_PositiveNegative_ExpectRetriesUntilTimeout_ThenFails()
    {
        await page.GotoAsync(website.BaseUrl + "/product/1");

        // Expect keeps checking for up to the timeout; the page never shows "Teapot", so it fails with a clear message.
        var error = await Assert.ThrowsAsync<PlaywrightException>(() =>
            Assertions.Expect(page.Locator("h2")).ToHaveTextAsync("Teapot", new() { Timeout = 500 }));

        Assert.Contains("Keyboard", error.Message); // it reports what it actually found
        // The original style reads ONCE, with no retry - right only if the page is already finished:
        Assert.Equal("Keyboard", await page.Locator("h2").InnerTextAsync());
    }

    [Fact]
    public async Task Waiting_PositiveNegative_ReadingAMissingElement_WaitsForTheTimeout()
    {
        await page.GotoAsync(website.BaseUrl + "/product/1");

        // InnerTextAsync waits for the element to appear; it never does -> TimeoutException after 300 ms
        // (default is 30 s, which is why a wrong selector makes a test suite slow).
        await Assert.ThrowsAsync<TimeoutException>(() => page.Locator("#does-not-exist").InnerTextAsync(new() { Timeout = 300 }));
    }

    [Fact]
    public async Task Selectors_NegativeNegative_AmbiguousLocator_IsRejectedByStrictMode()
    {
        await page.GotoAsync(website.BaseUrl + "/");

        // Every row has an "Edit" link. Acting on a locator that matches 2+ elements is an error ("strict mode"),
        // so a test never clicks the wrong one by accident. Fix: be more specific, or .First / .Nth(i) on purpose.
        var error = await Assert.ThrowsAsync<PlaywrightException>(() => page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync(new() { Timeout = 1000 }));

        Assert.Contains("strict mode violation", error.Message);
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).First.ClickAsync(); // explicit choice works
        await Assertions.Expect(page).ToHaveURLAsync(website.BaseUrl + "/Products/Edit/1");
    }
}
