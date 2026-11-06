using Microsoft.Playwright;
using PlaywrightBrowserTests.Fixtures;

namespace PlaywrightBrowserTests;

// Every behaviour has three kinds of test:
//   PositivePositive - VALID input            -> succeeds
//   PositiveNegative - well-formed input that breaks a rule (price 0, an id that doesn't exist) -> refused, nothing changes
//   NegativeNegative - MALFORMED or missing input (empty form, "abc" as a number or id)          -> rejected cleanly
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

    // For "page not found" checks we ask for the URL with Playwright's HTTP client (same cookies as the browser).
    // Chromium itself reports an empty 404 page as a navigation ERROR instead of a page, so page.GotoAsync can't be used.
    private async Task<int> GetStatusCodeAsync(string path)
    {
        IAPIResponse response = await page.APIRequest.GetAsync(website.BaseUrl + path);
        return response.Status;
    }

    private async Task<string> ReadPriceOnDetailsPageAsync(int productId)
    {
        await page.GotoAsync(website.BaseUrl + "/product/" + productId);
        return await page.Locator("p", new() { HasTextString = "Price:" }).InnerTextAsync();
    }

    private async Task<string> ReadAllFieldErrorsAsync()
    {
        IReadOnlyList<string> errorTexts = await page.Locator(".field-error").AllInnerTextsAsync();
        return string.Join(" ", errorTexts);
    }

    // ---------- Details page ----------

    [Fact]
    public async Task Details_PositivePositive_ExistingProductId_ShowsProduct()
    {
        IResponse? response = await page.GotoAsync(website.BaseUrl + "/product/1");

        Assert.Equal(200, response!.Status);
        Assert.Equal("Keyboard", await page.Locator("h2").InnerTextAsync());
    }

    [Fact]
    public async Task Details_PositiveNegative_WellFormedIdThatDoesNotExist_Returns404()
    {
        Assert.Equal(404, await GetStatusCodeAsync("/product/9999"));
    }

    [Fact]
    public async Task Details_NegativeNegative_NonNumericId_Returns404()
    {
        Assert.Equal(404, await GetStatusCodeAsync("/product/abc"));
    }

    // ---------- Create product ----------

    [Fact]
    public async Task Create_PositivePositive_ValidProduct_RedirectsToListAndShowsSuccessAlert()
    {
        // Unique name so this test never clashes with other tests or earlier runs.
        string productName = "Lamp " + Guid.NewGuid().ToString("N").Substring(0, 6);

        await page.GotoAsync(website.BaseUrl + "/Products/Create");
        await page.FillAsync("#Name", productName);
        await page.SelectOptionAsync("#Category", "Lighting");
        await page.FillAsync("#Price", "19.99");
        await page.ClickAsync("#save-button");

        await page.WaitForURLAsync(website.BaseUrl + "/");
        Assert.Equal("Products - Product Catalog", await page.TitleAsync());
        Assert.Contains(productName, await page.Locator("#success-alert").InnerTextAsync());
        Assert.Equal(1, await page.Locator("#product-table").GetByText(productName).CountAsync());
    }

    [Fact]
    public async Task Create_PositiveNegative_WellFormedButRuleBreakingValues_ShowsErrorsAndSavesNothing()
    {
        // "A" is a real name and 0 is a real number - but names need 2+ characters and prices start at 0.01.
        string productName = "A";

        await page.GotoAsync(website.BaseUrl + "/Products/Create");
        await page.FillAsync("#Name", productName);
        await page.SelectOptionAsync("#Category", "Lighting");
        await page.FillAsync("#Price", "0");
        await page.ClickAsync("#save-button");

        Assert.EndsWith("/Products/Create", page.Url);
        string errors = await ReadAllFieldErrorsAsync();
        Assert.Contains("Name must be 2 to 50 characters.", errors);
        Assert.Contains("Price must be between 0.01 and 100,000.", errors);
        Assert.Equal(0, await page.Locator("#success-alert").CountAsync());

        await page.GotoAsync(website.BaseUrl + "/");
        Assert.Equal(0, await page.Locator("#product-table td a", new() { HasTextRegex = new System.Text.RegularExpressions.Regex("^A$") }).CountAsync());
    }

    [Fact]
    public async Task Create_NegativeNegative_EmptyFormAndNonNumericPrice_ShowsErrorsAndStaysOnPage()
    {
        await page.GotoAsync(website.BaseUrl + "/Products/Create");
        await page.FillAsync("#Price", "abc");
        await page.ClickAsync("#save-button");

        Assert.EndsWith("/Products/Create", page.Url);
        string errors = await ReadAllFieldErrorsAsync();
        Assert.Contains("Name is required.", errors);
        Assert.Contains("Pick a category.", errors);
        Assert.Contains("is not valid for Price", errors);
        Assert.Equal(0, await page.Locator("#success-alert").CountAsync());
    }

    // ---------- Edit product ----------

    [Fact]
    public async Task Edit_PositivePositive_ValidNewPrice_SavesAndShowsSuccessAlert()
    {
        await page.GotoAsync(website.BaseUrl + "/Products/Edit/1");
        await page.FillAsync("#Price", "42.50");
        await page.ClickAsync("#save-button");

        Assert.Contains("was updated", await page.Locator("#success-alert").InnerTextAsync());
        Assert.Equal("Price: 42.50", await ReadPriceOnDetailsPageAsync(1));
    }

    [Fact]
    public async Task Edit_PositiveNegative_RuleBreakingPrice_ShowsErrorAndKeepsOldPrice()
    {
        string priceBefore = await ReadPriceOnDetailsPageAsync(2);

        await page.GotoAsync(website.BaseUrl + "/Products/Edit/2");
        await page.FillAsync("#Price", "0");
        await page.ClickAsync("#save-button");

        Assert.Contains("Price must be between 0.01 and 100,000.", await ReadAllFieldErrorsAsync());
        Assert.Equal(priceBefore, await ReadPriceOnDetailsPageAsync(2));
    }

    [Fact]
    public async Task Edit_NegativeNegative_NonNumericId_Returns404()
    {
        Assert.Equal(404, await GetStatusCodeAsync("/Products/Edit/abc"));
    }
}
