using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreQueryLogging.Variations;

// A tiny interceptor that only COUNTS the SQL commands EF sends - so the loading variations can be compared.
public class CommandCountingVariation : DbCommandInterceptor
{
    public int Commands { get; set; }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Commands++;
        return ValueTask.FromResult(result);
    }
}
