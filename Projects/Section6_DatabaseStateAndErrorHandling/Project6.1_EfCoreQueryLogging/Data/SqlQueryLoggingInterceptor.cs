using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCoreQueryLogging.Data;

// Runs around every SQL command EF sends. We log the exact SQL, its parameters and how long it took.
public class SqlQueryLoggingInterceptor : DbCommandInterceptor
{
    private const int SlowQueryMilliseconds = 200;
    private readonly ILogger<SqlQueryLoggingInterceptor> logger;

    public SqlQueryLoggingInterceptor(ILogger<SqlQueryLoggingInterceptor> logger)
    {
        this.logger = logger;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogCommand(command, eventData.Duration);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        LogCommand(command, eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogCommand(command, eventData.Duration);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        LogCommand(command, eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        logger.LogError(eventData.Exception, "SQL FAILED after {Milliseconds} ms: {Sql}", eventData.Duration.TotalMilliseconds, command.CommandText);
    }

    private void LogCommand(DbCommand command, TimeSpan duration)
    {
        string parameters = string.Join(", ", command.Parameters.Cast<DbParameter>().Select(parameter => $"{parameter.ParameterName}={parameter.Value}"));

        if (duration.TotalMilliseconds > SlowQueryMilliseconds)
        {
            // Slow queries are always worth seeing, even in production.
            logger.LogWarning("SLOW SQL {Milliseconds} ms: {Sql}", duration.TotalMilliseconds, command.CommandText);
        }
        else
        {
            // Normal queries only show at Debug level (turned off in production).
            logger.LogDebug("SQL {Milliseconds} ms [{Parameters}]\n{Sql}", duration.TotalMilliseconds, parameters, command.CommandText);
        }
    }
}
