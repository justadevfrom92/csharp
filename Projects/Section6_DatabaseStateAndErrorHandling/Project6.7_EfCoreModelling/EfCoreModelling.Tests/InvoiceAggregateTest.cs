using EfCoreModelling.DbContexts;
using EfCoreModelling.Models;
using EfCoreModelling.Services;
using EfCoreModelling.Tests.Helpers;

namespace EfCoreModelling.Tests;

// The invoice aggregate: a private lines list, AutoInclude, and the read-only view.
public class InvoiceAggregateTest : IDisposable
{
    private readonly ShopDatabaseHelper<ShopDbContext> shop = new(options => new ShopDbContext(options));

    public void Dispose()
    {
        shop.Dispose();
    }

    [Fact]
    public async Task Load_PositivePositive_AutoIncludeBringsTheLines_InOneQuery()
    {
        using ShopDbContext database = shop.NewContext();

        InvoiceModel? invoice = await new ProductSalesService(database).FindInvoiceAsync(shop.AcmeInvoiceId);

        Assert.Equal([25m, 40m, 3m], invoice!.Lines.Select(line => line.Amount));
        Assert.Single(shop.Sql);
        Assert.Contains("LEFT JOIN \"InvoiceLines\"", shop.Sql[0]);
    }

    [Fact]
    public async Task Load_PositiveNegative_ListPageSkipsTheLines_EvenAfterOneInvoiceWasLoaded()
    {
        using ShopDbContext database = shop.NewContext();
        var service = new ProductSalesService(database);
        await service.FindInvoiceAsync(shop.AcmeInvoiceId); // Acme's lines are now tracked by this DbContext
        shop.Sql.Clear();

        List<InvoiceModel> invoices = await service.ListInvoicesWithoutLinesAsync();

        Assert.All(invoices, invoice => Assert.Empty(invoice.Lines));
        Assert.DoesNotContain("InvoiceLines", shop.Sql.Single());
    }

    [Fact]
    public async Task Load_NegativeNegative_UnknownInvoice_IsNull()
    {
        using ShopDbContext database = shop.NewContext();

        Assert.Null(await new ProductSalesService(database).FindInvoiceAsync(Guid.NewGuid()));
    }

    [Fact]
    public void AddLine_PositivePositive_LineIsSavedThroughTheInvoice()
    {
        using (ShopDbContext database = shop.NewContext())
        {
            InvoiceModel invoice = database.Invoices.Single(invoice => invoice.PublicId == shop.AcmeInvoiceId);
            invoice.AddLine(database.Products.First(), 5m);
            database.SaveChanges();
        }

        using ShopDbContext again = shop.NewContext();
        Assert.Equal(4, again.Invoices.Single(invoice => invoice.PublicId == shop.AcmeInvoiceId).Lines.Count);
    }

    [Fact]
    public void AddLine_PositiveNegative_ListCanOnlyBeReadFromOutside()
    {
        var invoice = new InvoiceModel("Acme");

        Assert.Throws<NotSupportedException>(() => ((ICollection<InvoiceLineModel>)invoice.Lines).Add(new InvoiceLineModel(Guid.NewGuid(), 1m)));
        Assert.IsNotType<List<InvoiceLineModel>>(invoice.Lines); // can't be cast back to the real list
    }

    [Fact]
    public void AddLine_NegativeNegative_RulesRejectBadInput()
    {
        var invoice = new InvoiceModel("Acme");
        var cement = new MaterialModel("Cement", "kg");

        Assert.Throws<ArgumentOutOfRangeException>(() => invoice.AddLine(cement, 0m));
        Assert.Throws<ArgumentNullException>(() => invoice.AddLine(null!, 1m));
        Assert.Throws<ArgumentException>(() => new InvoiceModel(" "));
    }
}
