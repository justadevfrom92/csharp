using Microsoft.Playwright;
using PlaywrightBrowserTests.Fixtures;

namespace PlaywrightBrowserTests;

public class ProductCatalogBrowserTest : IClassFixture<ProductCatalogWebsiteFixture>, IAsyncLifetime
{
    private readonly ProductCatalogWebsiteFixture website;
    private IBrowserContext context = null!;
    private IPage page = null!;

    public ProductCatalogBrowserTest(ProductCatalogWebsiteFixture website)
    {
        this.website = website;
    }

    // Runs before EACH test: a brand-new browser context = fresh cookies, storage, and tab.
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
    public async Task Home_page_lists_products()
    {
        await page.GotoAsync(website.BaseUrl + "/");

        Assert.Equal("Products - Product Catalog", await page.TitleAsync());
        Assert.True(await page.Locator("#product-table tr").CountAsync() > 1);
    }

    [Fact]
    public async Task Adding_a_product_redirects_to_list_and_shows_success_alert()
    {
        // Unique name so this test never clashes with other tests or earlier runs.
        string productName = "Lamp " + Guid.NewGuid().ToString("N").Substring(0, 6);

        await page.GotoAsync(website.BaseUrl + "/Products/Create");
        await page.FillAsync("#Name", productName);
        await page.SelectOptionAsync("#Category", "Lighting");
        await page.FillAsync("#Price", "19.99");
        await page.ClickAsync("#save-button");

        await page.WaitForURLAsync(website.BaseUrl + "/");
        string alertText = await page.Locator("#success-alert").InnerTextAsync();
        Assert.Contains(productName, alertText);
        Assert.Equal(1, await page.Locator("#product-table").GetByText(productName).CountAsync());
    }

    [Fact]
    public async Task Empty_form_shows_validation_errors_and_stays_on_page()
    {
        await page.GotoAsync(website.BaseUrl + "/Products/Create");
        await page.ClickAsync("#save-button");

        Assert.EndsWith("/Products/Create", page.Url);
        IReadOnlyList<string> errorTexts = await page.Locator(".field-error").AllInnerTextsAsync();
        string errors = string.Join(" ", errorTexts);
        Assert.Contains("Name is required.", errors);
        Assert.Contains("Pick a category.", errors);
        Assert.Equal(0, await page.Locator("#success-alert").CountAsync());
    }

    [Fact]
    public async Task Clicking_edit_and_saving_updates_the_product()
    {
        await page.GotoAsync(website.BaseUrl + "/");
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).First.ClickAsync();

        await page.FillAsync("#Price", "42.50");
        await page.ClickAsync("#save-button");

        Assert.Contains("was updated", await page.Locator("#success-alert").InnerTextAsync());
    }
}
