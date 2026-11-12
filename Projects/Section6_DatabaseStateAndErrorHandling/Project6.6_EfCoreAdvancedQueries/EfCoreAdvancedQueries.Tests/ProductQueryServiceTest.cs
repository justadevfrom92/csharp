using EfCoreAdvancedQueries.DbContexts;
using EfCoreAdvancedQueries.Models;
using EfCoreAdvancedQueries.Outcomes;
using EfCoreAdvancedQueries.Services;
using EfCoreAdvancedQueries.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EfCoreAdvancedQueries.Tests;

// Seed data (see ShopDbContext): 1 Keyboard 25.00, 2 Mouse 12.50, 3 Monitor 180.00 (Computers),
// 4 Desk 220.00 (Furniture), 5 Old Webcam (Computers, discontinued), 6 Wobbly Chair (Furniture, discontinued).
//
// PositivePositive  = valid input -> it works
// PositiveNegative  = well-formed input that breaks a rule / matches nothing -> refused or nothing changes
// NegativeNegative  = broken or missing input -> rejected with an exception
public class ProductQueryServiceTest : IDisposable
{
    private readonly TestDatabaseHelper testDatabase = new TestDatabaseHelper();

    public void Dispose()
    {
        testDatabase.Dispose();
    }

    private ProductModel ReadProduct(int id)
    {
        // A fresh context, so we see what is REALLY in the database, not a cached object.
        using ShopDbContext context = testDatabase.CreateContext();
        return context.Products.AsNoTracking().Single(product => product.Id == id);
    }

    private int CountProducts()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        return context.Products.Count();
    }

    // ---------- FromSql ----------

    [Fact]
    public async Task SearchByName_PositivePositive_MatchingText_ReturnsMatchesSortedByName()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        List<ProductModel> found = await service.SearchByNameAsync("Mo");

        Assert.Equal(new[] { "Monitor", "Mouse" }, found.Select(product => product.Name));
    }

    [Fact]
    public async Task SearchByName_PositiveNegative_SqlInjectionText_IsTreatedAsPlainText()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        // If this were pasted into the SQL, "OR 1=1" would match EVERY row, and "; DROP TABLE" would delete it.
        List<ProductModel> found = await service.SearchByNameAsync("' OR 1=1; DROP TABLE Products; --");

        Assert.Empty(found);
        Assert.Equal(6, CountProducts()); // table still there, nothing deleted
    }

    [Fact]
    public async Task SearchByName_NegativeNegative_NullText_Throws()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SearchByNameAsync(null!));
    }

    // ---------- ExecuteSql ----------

    [Fact]
    public async Task Restock_PositivePositive_Category_AddsStockToActiveProductsOnly()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        int changed = await service.RestockAsync("Furniture", 5);

        Assert.Equal(1, changed);
        Assert.Equal(7, ReadProduct(4).Stock);  // Desk: 2 + 5
        Assert.Equal(1, ReadProduct(6).Stock);  // discontinued chair untouched
    }

    [Fact]
    public async Task Restock_PositiveNegative_UnknownCategory_ChangesNothing()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        int changed = await service.RestockAsync("Toys", 5);

        Assert.Equal(0, changed);
        Assert.Equal(2, ReadProduct(4).Stock);
    }

    [Theory]
    [InlineData("Furniture", 0)]
    [InlineData("Furniture", -3)]
    [InlineData("", 5)]
    [InlineData("   ", 5)]
    public async Task Restock_NegativeNegative_BadInput_Throws(string category, int extraUnits)
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.RestockAsync(category, extraUnits));
        Assert.Equal(2, ReadProduct(4).Stock);
    }

    // ---------- ExecuteUpdate ----------

    [Fact]
    public async Task RaisePrices_PositivePositive_Category_RaisesEveryActiveProductInOneGo()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        int changed = await service.RaisePricesAsync("Computers", 10);

        Assert.Equal(3, changed);
        Assert.Equal(2750, ReadProduct(1).PriceInCents);   // 25.00 -> 27.50
        Assert.Equal(1375, ReadProduct(2).PriceInCents);   // 12.50 -> 13.75
        Assert.Equal(19800, ReadProduct(3).PriceInCents);  // 180.00 -> 198.00
        Assert.Equal(1, ReadProduct(1).Version);           // version bumped, so open edit screens get a conflict
    }

    [Fact]
    public async Task RaisePrices_PositiveNegative_DiscontinuedAndOtherCategories_AreNotChanged()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        await service.RaisePricesAsync("Computers", 10);

        Assert.Equal(4000, ReadProduct(5).PriceInCents);   // discontinued webcam
        Assert.Equal(22000, ReadProduct(4).PriceInCents);  // Furniture desk
    }

    [Fact]
    public async Task RaisePrices_PositiveNegative_ObjectAlreadyLoaded_KeepsOldValueUntilReloaded()
    {
        // The big ExecuteUpdate gotcha: it changes the DATABASE, not objects you already loaded.
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);
        ProductModel keyboard = context.Products.Single(product => product.Id == 1);

        await service.RaisePricesAsync("Computers", 10);

        Assert.Equal(2500, keyboard.PriceInCents);         // the loaded object is now out of date
        await context.Entry(keyboard).ReloadAsync();
        Assert.Equal(2750, keyboard.PriceInCents);         // reload to see the real value
    }

    [Theory]
    [InlineData("Computers", 0)]
    [InlineData("Computers", 101)]
    [InlineData("", 10)]
    public async Task RaisePrices_NegativeNegative_BadInput_Throws(string category, int percent)
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.RaisePricesAsync(category, percent));
        Assert.Equal(2500, ReadProduct(1).PriceInCents);
    }

    // ---------- ExecuteDelete ----------

    [Fact]
    public async Task DeleteDiscontinued_PositivePositive_Category_DeletesOnlyDiscontinuedRows()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        int deleted = await service.DeleteDiscontinuedAsync("Computers");

        Assert.Equal(1, deleted);
        Assert.Equal(5, CountProducts());
        Assert.Null(await service.GetByIdAsync(5));        // webcam gone
        Assert.NotNull(await service.GetByIdAsync(6));     // the discontinued chair is Furniture: kept
    }

    [Fact]
    public async Task DeleteDiscontinued_PositiveNegative_CategoryWithNoneDiscontinued_DeletesNothing()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        int deleted = await service.DeleteDiscontinuedAsync("Toys");

        Assert.Equal(0, deleted);
        Assert.Equal(6, CountProducts());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DeleteDiscontinued_NegativeNegative_MissingCategory_ThrowsAndDeletesNothing(string? category)
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.DeleteDiscontinuedAsync(category!));
        Assert.Equal(6, CountProducts());
    }

    // ---------- Compiled query ----------

    [Fact]
    public async Task GetById_PositivePositive_ExistingId_ReturnsProduct()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        ProductModel? product = await service.GetByIdAsync(1);

        Assert.NotNull(product);
        Assert.Equal("Keyboard", product.Name);
    }

    [Fact]
    public async Task GetById_PositiveNegative_UnknownId_ReturnsNull()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        Assert.Null(await service.GetByIdAsync(999));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetById_NegativeNegative_IdNotPositive_Throws(int id)
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetByIdAsync(id));
    }

    // ---------- Concurrency token ----------

    [Fact]
    public async Task UpdatePrice_PositivePositive_VersionStillCurrent_Saves()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        PriceUpdateOutcome outcome = await service.UpdatePriceAsync(1, 2999, versionTheUserSaw: 0);

        Assert.Equal(PriceUpdateOutcome.Updated, outcome);
        Assert.Equal(2999, ReadProduct(1).PriceInCents);
        Assert.Equal(1, ReadProduct(1).Version);
    }

    [Fact]
    public async Task UpdatePrice_PositiveNegative_TwoUsersSaveTheSameProduct_SecondGetsConflict()
    {
        // Alice and Bob each have their OWN DbContext (like two web requests) and both saw version 0.
        using ShopDbContext aliceContext = testDatabase.CreateContext();
        using ShopDbContext bobContext = testDatabase.CreateContext();
        var alice = new ProductQueryService(aliceContext);
        var bob = new ProductQueryService(bobContext);

        PriceUpdateOutcome aliceOutcome = await alice.UpdatePriceAsync(1, 2999, versionTheUserSaw: 0);
        PriceUpdateOutcome bobOutcome = await bob.UpdatePriceAsync(1, 1999, versionTheUserSaw: 0);

        Assert.Equal(PriceUpdateOutcome.Updated, aliceOutcome);
        Assert.Equal(PriceUpdateOutcome.ConflictSomeoneElseChangedIt, bobOutcome);
        Assert.Equal(2999, ReadProduct(1).PriceInCents);   // Alice's price was NOT overwritten
    }

    [Fact]
    public async Task UpdatePrice_PositiveNegative_BulkUpdateHappenedInBetween_GetsConflict()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);
        await service.RaisePricesAsync("Computers", 10);   // bumps Version to 1

        PriceUpdateOutcome outcome = await service.UpdatePriceAsync(1, 1999, versionTheUserSaw: 0);

        Assert.Equal(PriceUpdateOutcome.ConflictSomeoneElseChangedIt, outcome);
        Assert.Equal(2750, ReadProduct(1).PriceInCents);
    }

    [Fact]
    public async Task UpdatePrice_PositiveNegative_UnknownId_ReturnsNotFound()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        Assert.Equal(PriceUpdateOutcome.NotFound, await service.UpdatePriceAsync(999, 1999, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public async Task UpdatePrice_NegativeNegative_PriceNotPositive_ThrowsAndChangesNothing(long newPriceInCents)
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var service = new ProductQueryService(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.UpdatePriceAsync(1, newPriceInCents, 0));
        Assert.Equal(2500, ReadProduct(1).PriceInCents);
    }
}
