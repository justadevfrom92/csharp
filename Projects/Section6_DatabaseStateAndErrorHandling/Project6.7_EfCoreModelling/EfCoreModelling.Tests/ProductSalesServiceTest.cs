using EfCoreModelling.DbContexts;
using EfCoreModelling.Models;
using EfCoreModelling.Services;
using EfCoreModelling.Tests.Helpers;

namespace EfCoreModelling.Tests;

// The video's query: OfType + Join + GroupBy, all in ONE SQL command.
public class ProductSalesServiceTest : IDisposable
{
    private readonly ShopDatabaseHelper<ShopDbContext> shop = new(options => new ShopDbContext(options));

    public void Dispose()
    {
        shop.Dispose();
    }

    [Fact]
    public async Task Consumption_PositivePositive_SummedPerMaterial_InTheDatabase()
    {
        using ShopDbContext database = shop.NewContext();

        ProductConsumptionModel[] sold = await new ProductSalesService(database).GetMaterialConsumptionAsync();

        Assert.Equal([("Cable", 40m), ("Cement", 35m)], sold.Select(row => (row.Name, row.AmountSold)));
        Assert.Equal(shop.CementId, sold[1].ProductId);
        string sql = Assert.Single(shop.Sql);
        Assert.Contains("WHERE \"p\".\"Type\" = 'Material'", sql); // OfType<MaterialModel>()
        Assert.Contains("INNER JOIN \"InvoiceLines\"", sql);
        Assert.Contains("GROUP BY", sql);
    }

    [Fact]
    public async Task Consumption_PositiveNegative_ServicesAndUnsoldMaterialsAreLeftOut()
    {
        using (ShopDbContext setup = shop.NewContext())
        {
            setup.Products.Add(new MaterialModel("Sand", "kg")); // never sold
            setup.SaveChanges();
        }
        using ShopDbContext database = shop.NewContext();

        ProductConsumptionModel[] sold = await new ProductSalesService(database).GetMaterialConsumptionAsync();

        Assert.DoesNotContain(sold, row => row.Name is "Installation" or "Sand"); // INNER JOIN: no lines, no row
    }

    [Fact]
    public async Task Consumption_NegativeNegative_NoInvoiceLines_EmptyArray()
    {
        shop.Query("DELETE FROM InvoiceLines");
        using ShopDbContext database = shop.NewContext();

        Assert.Empty(await new ProductSalesService(database).GetMaterialConsumptionAsync());
    }
}
