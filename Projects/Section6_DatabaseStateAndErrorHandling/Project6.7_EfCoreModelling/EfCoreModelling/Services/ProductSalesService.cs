using EfCoreModelling.DbContexts;
using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.Services;

public class ProductSalesService(ShopDbContext database)
{
    // "How much of each material was sold?" - the video's query. All of it runs in the DATABASE:
    //   OfType<MaterialModel>()  -> WHERE Type = 'Material'
    //   Join(... PublicId == ProductId) -> INNER JOIN InvoiceLines
    //   GroupBy(..., (key, group) => ...) -> GROUP BY, with SUM(Amount)
    public async Task<ProductConsumptionModel[]> GetMaterialConsumptionAsync()
    {
        return await database.Products.OfType<MaterialModel>()
            .Join(database.InvoiceLines, material => material.PublicId, line => line.ProductId,
                (material, line) => new { Material = material, line.Amount })
            .GroupBy(
                pair => new { pair.Material.PublicId, pair.Material.Name },
                (key, group) => new ProductConsumptionModel
                {
                    ProductId = key.PublicId,
                    Name = key.Name,
                    AmountSold = group.Sum(pair => pair.Amount),
                })
            .OrderBy(consumption => consumption.Name)
            .ToArrayAsync();
    }

    // AutoInclude: the lines come along without Include().
    public Task<InvoiceModel?> FindInvoiceAsync(Guid publicId)
    {
        return database.Invoices.FirstOrDefaultAsync(invoice => invoice.PublicId == publicId);
    }

    // IgnoreAutoIncludes: switch it off for ONE query (a list page that only shows customers).
    // AsNoTracking too: a list page changes nothing, and without it lines the SAME DbContext loaded earlier get
    // attached to these invoices anyway (see Variations/TrackedListVariation).
    public Task<List<InvoiceModel>> ListInvoicesWithoutLinesAsync()
    {
        return database.Invoices.IgnoreAutoIncludes().AsNoTracking().ToListAsync();
    }
}
