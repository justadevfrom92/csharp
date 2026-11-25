using EfCoreModelling.DbContexts;
using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.Variations;

// IgnoreAutoIncludes WITHOUT AsNoTracking. The query doesn't load lines - but if this DbContext already loaded an
// invoice's lines earlier, EF attaches ("fixes up") them to the invoices it returns now. What you see depends on
// what ran before. ProductSalesService.ListInvoicesWithoutLinesAsync (original) adds AsNoTracking().
public static class TrackedListVariation
{
    public static Task<List<InvoiceModel>> ListInvoicesAsync(ShopDbContext database)
    {
        return database.Invoices.IgnoreAutoIncludes().ToListAsync();
    }
}
