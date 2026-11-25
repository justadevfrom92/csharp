using EfCoreModelling.DbContexts;
using EfCoreModelling.Models;
using EfCoreModelling.Services;
using EfCoreModelling.Tests.Helpers;
using EfCoreModelling.Variations;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.Tests;

public class VariationsTest
{
    [Fact]
    public void Inline_PositivePositive_IncludeByNameLoadsThePrivateList()
    {
        using var shop = new ShopDatabaseHelper<InlineModelDbContextVariation>(options => new InlineModelDbContextVariation(options));
        using InlineModelDbContextVariation database = shop.NewContext();

        InvoiceModel invoice = database.Invoices.Include("linesCollection").Single(invoice => invoice.PublicId == shop.AcmeInvoiceId);

        Assert.Equal(3, invoice.Lines.Count);
    }

    [Fact]
    public void Inline_PositiveNegative_WithoutAutoInclude_LinesAreSilentlyEmpty()
    {
        using var shop = new ShopDatabaseHelper<InlineModelDbContextVariation>(options => new InlineModelDbContextVariation(options));
        using InlineModelDbContextVariation database = shop.NewContext();

        InvoiceModel invoice = database.Invoices.Single(invoice => invoice.PublicId == shop.AcmeInvoiceId);

        Assert.Empty(invoice.Lines); // no error - the invoice just looks like it has no lines
    }

    [Fact]
    public void Inline_NegativeNegative_IncludeWithAWrongName_Throws()
    {
        using var shop = new ShopDatabaseHelper<InlineModelDbContextVariation>(options => new InlineModelDbContextVariation(options));
        using InlineModelDbContextVariation database = shop.NewContext();

        Assert.Throws<InvalidOperationException>(() => database.Invoices.Include("Lines").ToList()); // a string: only checked when it runs
    }

    [Fact]
    public void Scanned_PositivePositive_FindsAllConfigurations_SameModelAsTheOriginal()
    {
        using var scanned = new ShopDatabaseHelper<ScannedConfigurationsDbContextVariation>(options => new ScannedConfigurationsDbContextVariation(options));
        using var original = new ShopDatabaseHelper<ShopDbContext>(options => new ShopDbContext(options));
        using ScannedConfigurationsDbContextVariation scannedDatabase = scanned.NewContext();
        using ShopDbContext originalDatabase = original.NewContext();

        List<string> Names(DbContext database)
        {
            return database.Model.GetEntityTypes().Select(type => type.ClrType.Name).Order().ToList();
        }

        Assert.Equal(Names(originalDatabase), Names(scannedDatabase));
        Assert.Equal(3, scannedDatabase.Invoices.Single(invoice => invoice.PublicId == scanned.AcmeInvoiceId).Lines.Count); // AutoInclude came along
    }

    [Fact]
    public void TablePerType_PositivePositive_ATableForEachKind_NoTypeColumn()
    {
        using var shop = new ShopDatabaseHelper<TablePerTypeDbContextVariation>(options => new TablePerTypeDbContextVariation(options));

        // "InvoiceModel", not "Invoices": a table is named after its DbSet property, and this context has no Invoices
        // DbSet - so the CLASS name is used. Add a DbSet or .ToTable("Invoices") to keep the name.
        Assert.Equal(["InvoiceLines", "InvoiceModel", "Materials", "Products", "Services"], shop.Query("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name"));
        Assert.DoesNotContain(shop.Query("SELECT name FROM pragma_table_info('Products')"), column => column == "Type");
        Assert.Equal(["kg", "m"], shop.Query("SELECT Unit FROM Materials ORDER BY Id"));
    }

    [Fact]
    public void TablePerType_PositiveNegative_OfTypeBecomesAJoinInsteadOfAWhere()
    {
        using var shop = new ShopDatabaseHelper<TablePerTypeDbContextVariation>(options => new TablePerTypeDbContextVariation(options));
        using TablePerTypeDbContextVariation database = shop.NewContext();

        Assert.Equal(2, database.Products.OfType<MaterialModel>().Count());
        Assert.Contains("JOIN \"Materials\"", shop.Sql.Single());
        Assert.DoesNotContain("\"Type\"", shop.Sql.Single());
    }

    [Fact]
    public void Leaky_NegativeNegative_CastBackToListSkipsTheRules()
    {
        var invoice = new LeakyInvoiceVariation();

        ((List<InvoiceLineModel>)invoice.Lines).Add(new InvoiceLineModel(Guid.NewGuid(), -50m)); // AddLine would refuse -50

        Assert.Equal(-50m, invoice.Lines.Single().Amount);
        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.AddLine(new MaterialModel("Cement", "kg"), -50m));
    }

    [Fact]
    public async Task QueryStyles_PositivePositive_AllThreeGiveTheSameAnswer()
    {
        using var shop = new ShopDatabaseHelper<ShopDbContext>(options => new ShopDbContext(options));
        using ShopDbContext database = shop.NewContext();

        ProductConsumptionModel[] methodSyntax = await new ProductSalesService(database).GetMaterialConsumptionAsync();

        Assert.Equal(methodSyntax, await QueryStyleVariation.WithQuerySyntaxAsync(database));
        Assert.Equal(methodSyntax, await QueryStyleVariation.InMemoryAsync(database));
    }

    [Fact]
    public async Task QueryStyles_PositiveNegative_InMemoryNeedsTwoQueries_AndFetchesEveryLine()
    {
        using var shop = new ShopDatabaseHelper<ShopDbContext>(options => new ShopDbContext(options));
        using ShopDbContext database = shop.NewContext();

        await QueryStyleVariation.WithQuerySyntaxAsync(database);
        List<string> querySyntaxSql = [.. shop.Sql];
        shop.Sql.Clear();
        await QueryStyleVariation.InMemoryAsync(database);

        Assert.Contains("GROUP BY", Assert.Single(querySyntaxSql)); // same single SQL as method syntax
        Assert.Equal(2, shop.Sql.Count);
        Assert.DoesNotContain(shop.Sql, sql => sql.Contains("GROUP BY"));
    }

    [Fact]
    public async Task TrackedList_PositiveNegative_LinesLoadedEarlierShowUpAnyway()
    {
        using var shop = new ShopDatabaseHelper<ShopDbContext>(options => new ShopDbContext(options));
        using ShopDbContext database = shop.NewContext();
        await new ProductSalesService(database).FindInvoiceAsync(shop.AcmeInvoiceId);

        List<InvoiceModel> invoices = await TrackedListVariation.ListInvoicesAsync(database);

        Assert.Equal(3, invoices.Single(invoice => invoice.Customer == "Acme").Lines.Count); // "ignored" lines are there
        Assert.Empty(invoices.Single(invoice => invoice.Customer == "Globex").Lines);
        using ShopDbContext fresh = shop.NewContext();
        Assert.All(await TrackedListVariation.ListInvoicesAsync(fresh), invoice => Assert.Empty(invoice.Lines)); // depends on history
    }
}
