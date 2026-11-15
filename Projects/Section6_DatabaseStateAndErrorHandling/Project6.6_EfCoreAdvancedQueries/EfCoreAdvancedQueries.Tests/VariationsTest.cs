using EfCoreAdvancedQueries.DbContexts;
using EfCoreAdvancedQueries.Models;
using EfCoreAdvancedQueries.Outcomes;
using EfCoreAdvancedQueries.Services;
using EfCoreAdvancedQueries.Tests.Helpers;
using EfCoreAdvancedQueries.Variations;
using Microsoft.EntityFrameworkCore;

namespace EfCoreAdvancedQueries.Tests;

// Each variation compared with ProductQueryService (the original), on a real in-memory SQLite database.
//   PositivePositive = valid input -> same result
//   PositiveNegative = valid input where a variation behaves DIFFERENTLY (the point of the comparison)
//   NegativeNegative = hostile/broken input -> safe versions refuse it, the wrong ones don't
public class VariationsTest : IDisposable
{
    private readonly TestDatabaseHelper testDatabase = new TestDatabaseHelper();

    public void Dispose()
    {
        testDatabase.Dispose();
    }

    private static List<string> Names(List<ProductModel> products)
    {
        return products.Select(product => product.Name).OrderBy(name => name).ToList();
    }

    private ProductModel Read(int id)
    {
        using ShopDbContext context = testDatabase.CreateContext();
        return context.Products.AsNoTracking().Single(product => product.Id == id);
    }

    // ---------- search ----------

    [Fact]
    public async Task Search_PositivePositive_NormalText_AllFiveWaysFindTheSameProducts()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var original = new ProductQueryService(context);
        var variations = new ProductQueryVariation(context);

        List<string> expected = Names(await original.SearchByNameAsync("Mo"));

        Assert.Equal(["Monitor", "Mouse"], expected);
        Assert.Equal(expected, Names(await variations.SearchWithFromSqlInterpolated("Mo")));
        Assert.Equal(expected, Names(await variations.SearchWithFromSqlRawAndParameter("Mo")));
        Assert.Equal(expected, Names(await variations.SearchWithStringConcatenation("Mo")));
        Assert.Equal(expected, Names(await variations.SearchWithLinq("Mo")));
    }

    [Fact]
    public async Task Search_NegativeNegative_InjectionText_SafeVersionsFindNothing_ConcatenationReturnsEveryRow()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var variations = new ProductQueryVariation(context);
        const string attack = "' OR 1=1 --";

        Assert.Empty(await new ProductQueryService(context).SearchByNameAsync(attack));
        Assert.Empty(await variations.SearchWithFromSqlInterpolated(attack));
        Assert.Empty(await variations.SearchWithFromSqlRawAndParameter(attack));
        Assert.Empty(await variations.SearchWithLinq(attack));
        Assert.Equal(6, (await variations.SearchWithStringConcatenation(attack)).Count); // SQL INJECTION: all 6 products
    }

    [Fact]
    public async Task Search_PositiveNegative_QuoteInANormalName_BreaksOnlyTheConcatenatedSql()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var variations = new ProductQueryVariation(context);

        Assert.Empty(await variations.SearchWithLinq("O'Brien"));                       // fine: just no match
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => variations.SearchWithStringConcatenation("O'Brien")); // syntax error
    }

    // ---------- bulk update ----------

    [Fact]
    public async Task RaisePrices_PositivePositive_LoadAndSave_GivesTheSamePricesAsExecuteUpdate()
    {
        using (ShopDbContext context = testDatabase.CreateContext())
        {
            Assert.Equal(3, await new ProductQueryVariation(context).RaisePricesWithLoadAndSave("Computers", 10));
        }

        Assert.Equal(2750, Read(1).PriceInCents);
        Assert.Equal(1375, Read(2).PriceInCents);
        Assert.Equal(1, Read(1).Version);
    }

    [Fact]
    public async Task RaisePrices_PositiveNegative_LoadAndSave_KeepsLoadedObjectsUpToDate_ExecuteUpdateDoesNot()
    {
        using ShopDbContext first = testDatabase.CreateContext();
        ProductModel keyboardA = first.Products.Single(product => product.Id == 1);
        await new ProductQueryVariation(first).RaisePricesWithLoadAndSave("Computers", 10);

        using ShopDbContext second = testDatabase.CreateContext();
        ProductModel keyboardB = second.Products.Single(product => product.Id == 1);
        await new ProductQueryService(second).RaisePricesAsync("Computers", 10);

        Assert.Equal(2750, keyboardA.PriceInCents); // changed through the change tracker: the object IS the change
        Assert.Equal(2750, keyboardB.PriceInCents); // ExecuteUpdate went straight to the database: object is stale
        Assert.Equal(3025, Read(1).PriceInCents);   // the database had both raises
    }

    // ---------- concurrency ----------

    [Fact]
    public async Task Concurrency_PositivePositive_VersionInWhere_ProtectsLikeTheToken()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var variations = new ProductQueryVariation(context);

        int alice = await variations.UpdatePriceWithVersionInWhere(1, 2999, versionTheUserSaw: 0);
        int bob = await variations.UpdatePriceWithVersionInWhere(1, 1999, versionTheUserSaw: 0);

        Assert.Equal(1, alice);
        Assert.Equal(0, bob);                        // 0 rows = conflict
        Assert.Equal(2999, Read(1).PriceInCents);
    }

    [Fact]
    public async Task Concurrency_PositiveNegative_LastWriteWins_SilentlyLosesTheFirstChange()
    {
        using ShopDbContext aliceContext = testDatabase.CreateContext();
        using ShopDbContext bobContext = testDatabase.CreateContext();

        await new ProductQueryVariation(aliceContext).UpdatePriceWithoutVersionCheck(1, 2999);
        int bobRows = await new ProductQueryVariation(bobContext).UpdatePriceWithoutVersionCheck(1, 1999);

        Assert.Equal(1, bobRows);                     // no error...
        Assert.Equal(1999, Read(1).PriceInCents);     // ...and Alice's 29.99 is gone
        // The original refuses Bob: UpdatePriceAsync returns ConflictSomeoneElseChangedIt (see ProductQueryServiceTest).
    }

    [Fact]
    public async Task Concurrency_NegativeNegative_UnknownId_NoRowsChangedEitherWay()
    {
        using ShopDbContext context = testDatabase.CreateContext();
        var variations = new ProductQueryVariation(context);

        Assert.Equal(0, await variations.UpdatePriceWithoutVersionCheck(999, 1));
        Assert.Equal(0, await variations.UpdatePriceWithVersionInWhere(999, 1, 0));
        Assert.Equal(PriceUpdateOutcome.NotFound, await new ProductQueryService(context).UpdatePriceAsync(999, 1, 0));
    }
}
