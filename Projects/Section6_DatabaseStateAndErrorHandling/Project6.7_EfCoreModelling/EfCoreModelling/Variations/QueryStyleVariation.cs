using EfCoreModelling.DbContexts;
using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.Variations;

// The "material sold" query written two other ways. ProductSalesService.GetMaterialConsumptionAsync (original)
// uses method syntax; all three give the same answer.
public static class QueryStyleVariation
{
    // QUERY SYNTAX (join ... on ... equals, group ... by): the same SQL, reads more like SQL.
    public static Task<ProductConsumptionModel[]> WithQuerySyntaxAsync(ShopDbContext database)
    {
        IQueryable<ProductConsumptionModel> query =
            from material in database.Products.OfType<MaterialModel>()
            join line in database.InvoiceLines on material.PublicId equals line.ProductId
            group line.Amount by new { material.PublicId, material.Name } into sold
            orderby sold.Key.Name
            select new ProductConsumptionModel { ProductId = sold.Key.PublicId, Name = sold.Key.Name, AmountSold = sold.Sum() };
        return query.ToArrayAsync();
    }

    // LOAD EVERYTHING, then LINQ in memory: two queries that bring back EVERY material and EVERY line.
    // Same answer here; with a million lines, a million rows travel instead of one per material.
    public static async Task<ProductConsumptionModel[]> InMemoryAsync(ShopDbContext database)
    {
        List<MaterialModel> materials = await database.Products.OfType<MaterialModel>().ToListAsync();
        List<InvoiceLineModel> lines = await database.InvoiceLines.ToListAsync();
        return materials
            .Join(lines, material => material.PublicId, line => line.ProductId, (material, line) => new { material, line.Amount })
            .GroupBy(pair => pair.material, (material, group) => new ProductConsumptionModel
            {
                ProductId = material.PublicId,
                Name = material.Name,
                AmountSold = group.Sum(pair => pair.Amount),
            })
            .OrderBy(consumption => consumption.Name)
            .ToArray();
    }
}
