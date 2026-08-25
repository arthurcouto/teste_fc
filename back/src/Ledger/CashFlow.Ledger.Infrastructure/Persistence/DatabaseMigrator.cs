using System.Reflection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CashFlow.Ledger.Infrastructure.Persistence;

public sealed partial class DatabaseMigrator(
    NpgsqlDataSource dataSource,
    LedgerInfrastructureOptions options,
    ILogger<DatabaseMigrator> logger)
{
    private const string HistoryTable = "schema_migration";
    private static readonly TimeSpan IndexJobTimeout = TimeSpan.FromMinutes(5);
    private static readonly string[] ConcurrentDdlStates = ["23505", "42701", "42P06", "42P07", "42710"];

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        await EnsureHistoryTableAsync(cancellationToken);

        var applied = await AppliedMigrationsAsync(cancellationToken);

        foreach (var (name, script) in EmbeddedMigrations())
        {
            if (applied.Contains(name))
            {
                continue;
            }

            Log.Applying(logger, name);

            foreach (var raw in SqlStatements.Split(script))
            {
                var statement = SqlStatements.ForEngine(raw, options.Engine);
                var jobId = await ExecuteAsync(statement, cancellationToken);

                if (jobId is not null)
                {
                    await WaitForIndexJobAsync(jobId, cancellationToken);
                }
            }

            await RecordAppliedAsync(name, cancellationToken);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Applying migration {migration}")]
        public static partial void Applying(ILogger logger, string migration);

        [LoggerMessage(Level = LogLevel.Information, Message = "Index job {jobId} completed")]
        public static partial void IndexCompleted(ILogger logger, string jobId);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Statement already applied by a concurrent replica ({sqlState}): {statement}")]
        public static partial void ConcurrentlyApplied(ILogger logger, string? sqlState, string statement);
    }

    private static IEnumerable<(string Name, string Script)> EmbeddedMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = $"{assembly.GetName().Name}.Migrations.";

        return assembly.GetManifestResourceNames()
            .Where(resource => resource.StartsWith(prefix, StringComparison.Ordinal)
                               && resource.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(resource => resource, StringComparer.Ordinal)
            .Select(resource =>
            {
                using var stream = assembly.GetManifestResourceStream(resource)!;
                using var reader = new StreamReader(stream);
                return (resource[prefix.Length..^4], reader.ReadToEnd());
            });
    }

    private async Task EnsureHistoryTableAsync(CancellationToken cancellationToken) =>
        await ExecuteAsync(
            $"CREATE TABLE IF NOT EXISTS {HistoryTable} (name VARCHAR(200) PRIMARY KEY, applied_at TIMESTAMPTZ NOT NULL)",
            cancellationToken);

    private async Task<HashSet<string>> AppliedMigrationsAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand($"SELECT name FROM {HistoryTable}");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var applied = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(reader.GetString(0));
        }

        return applied;
    }

    private async Task RecordAppliedAsync(string name, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            $"INSERT INTO {HistoryTable} (name, applied_at) VALUES ($1, now()) ON CONFLICT (name) DO NOTHING");
        command.Parameters.AddWithValue(name);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<string?> ExecuteAsync(string statement, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(statement);

        try
        {
            if (options.Engine is DatabaseEngine.PostgreSql || !SqlStatements.IsAsynchronousIndex(statement))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
                return null;
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result as string;
        }
        catch (PostgresException exception) when (ConcurrentDdlStates.Contains(exception.SqlState))
        {
            Log.ConcurrentlyApplied(logger, exception.SqlState, statement);
            return null;
        }
    }

    private async Task WaitForIndexJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(IndexJobTimeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var command = dataSource.CreateCommand("SELECT status FROM sys.jobs WHERE job_id = $1");
            command.Parameters.AddWithValue(jobId);

            var status = await command.ExecuteScalarAsync(cancellationToken) as string;

            switch (status)
            {
                case "completed":
                    Log.IndexCompleted(logger, jobId);
                    return;
                case "failed":
                    throw new MigrationFailedException($"Index job {jobId} failed.");
                default:
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    break;
            }
        }

        throw new MigrationFailedException($"Index job {jobId} did not finish within {IndexJobTimeout}.");
    }
}
