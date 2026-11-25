using EfCoreModelling.DbContexts;
using EfCoreModelling.Models;
using EfCoreModelling.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.Tests;

// Table-per-hierarchy (one Products table + Type column) and shadow keys/foreign keys.
public class ModelTest : IDisposable
{
    private readonly ShopDatabaseHelper<ShopDbContext> shop = new(options => new ShopDbContext(options));

    public void Dispose()
    {
        shop.Dispose();
    }

    [Fact]
    public void Hierarchy_PositivePositive_OneTableWithATypeColumn()
    {
        Assert.Equal(["InvoiceLines", "Invoices", "Products"], shop.Query("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name"));
        Assert.Equal(["Cement|Material", "Cable|Material", "Installation|Service"], shop.Query("SELECT Name, Type FROM Products ORDER BY Id"));
    }

    [Fact]
    public void Hierarchy_PositiveNegative_EachKindLeavesTheOthersColumnsEmpty()
    {
        Assert.Equal(["Cement|kg|NULL", "Cable|m|NULL", "Installation|NULL|85.0"], shop.Query("SELECT Name, Unit, HourlyRate FROM Products ORDER BY Id"));
    }

    [Fact]
    public void Hierarchy_NegativeNegative_UnknownTypeInTheDatabase_CannotBeLoaded()
    {
        shop.Query("INSERT INTO Products (PublicId, Name, Type) VALUES ('5f1c0a1e-0000-0000-0000-000000000001', 'Mystery', 'Gadget')");
        using ShopDbContext database = shop.NewContext();

        Assert.Throws<InvalidOperationException>(() => database.Products.ToList()); // no class for "Gadget"
    }

    [Fact]
    public void Shadow_PositivePositive_IdAndInvoiceIdExistOnlyInTheDatabase()
    {
        using ShopDbContext database = shop.NewContext();

        List<int> productIds = database.Products.Select(product => EF.Property<int>(product, "Id")).OrderBy(id => id).ToList();
        List<int> lineInvoiceIds = database.InvoiceLines.Select(line => EF.Property<int>(line, "InvoiceId")).OrderBy(id => id).ToList();

        Assert.Equal([1, 2, 3], productIds);
        Assert.Equal([1, 1, 1, 2], lineInvoiceIds);
    }

    [Fact]
    public void Shadow_PositiveNegative_TheClassesHaveNoIdProperty_ButEntryCanReadIt()
    {
        using ShopDbContext database = shop.NewContext();
        InvoiceModel invoice = database.Invoices.First(invoice => invoice.Customer == "Acme");

        Assert.Null(typeof(InvoiceModel).GetProperty("Id"));
        Assert.Null(typeof(InvoiceLineModel).GetProperty("InvoiceId"));
        Assert.Equal(1, database.Entry(invoice).Property<int>("Id").CurrentValue); // on a TRACKED object: Entry(...)
    }

    [Fact]
    public void Shadow_NegativeNegative_EfPropertyOutsideAQuery_Throws()
    {
        using ShopDbContext database = shop.NewContext();
        InvoiceModel invoice = database.Invoices.First();

        Assert.Throws<InvalidOperationException>(() => EF.Property<int>(invoice, "Id"));
    }
}
