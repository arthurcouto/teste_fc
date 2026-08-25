using System.Data;
using CashFlow.Ledger.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CashFlow.Ledger.Infrastructure.Persistence;

internal sealed partial class UnitOfWork(
    LedgerDbContext context,
    LedgerInfrastructureOptions options,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private const string SerializationFailure = "40001";
    private const string DeadlockDetected = "40P01";

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        for (var attempt = 1; ; attempt++)
        {
            context.ChangeTracker.Clear();
            context.Database.AutoSavepointsEnabled = false;

            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.RepeatableRead, cancellationToken);

            try
            {
                var result = await operation(cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception exception) when (IsTransient(exception) && attempt < options.TransactionMaxAttempts)
            {
                Log.Conflict(logger, attempt, exception.Message);
                await Task.Delay(BackoffFor(attempt), cancellationToken);
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Warning, Message = "Transaction conflict on attempt {attempt}; retrying: {reason}")]
        public static partial void Conflict(ILogger logger, int attempt, string reason);
    }

    private static bool IsTransient(Exception exception) =>
        IsConcurrencyConflict(exception)
        || IsConcurrencyConflict(exception.InnerException)
        || exception is NpgsqlException { IsTransient: true };

    private static bool IsConcurrencyConflict(Exception? exception) =>
        exception is PostgresException { SqlState: SerializationFailure or DeadlockDetected };

    private static TimeSpan BackoffFor(int attempt)
    {
        var ceiling = Math.Min(25 * Math.Pow(2, attempt - 1), 1000);
        return TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * ceiling);
    }
}
