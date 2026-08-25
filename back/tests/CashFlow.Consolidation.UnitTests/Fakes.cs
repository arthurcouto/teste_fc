using CashFlow.Consolidation.Application;

namespace CashFlow.Consolidation.UnitTests;

internal sealed class InMemoryBalances : IDailyBalanceRepository
{
    private readonly Dictionary<DateOnly, DailyBalance> _balances = [];

    public int SaveCount { get; private set; }

    public IReadOnlyDictionary<DateOnly, DailyBalance> Snapshot => _balances;

    public void RestoreFrom(IReadOnlyDictionary<DateOnly, DailyBalance> snapshot)
    {
        _balances.Clear();
        foreach (var (date, balance) in snapshot)
        {
            _balances[date] = balance;
        }
    }

    public Task<DailyBalance?> FindAsync(DateOnly competenceDate, CancellationToken cancellationToken) =>
        Task.FromResult(_balances.GetValueOrDefault(competenceDate));

    public Task<IReadOnlyList<DailyBalance>> ListAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DailyBalance>>(
            [.. _balances.Values.Where(b => b.CompetenceDate >= startDate && b.CompetenceDate <= endDate)]);

    public Task SaveAsync(DailyBalance balance, CancellationToken cancellationToken)
    {
        _balances[balance.CompetenceDate] = balance;
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryProcessedEntries : IProcessedEntryLog
{
    private readonly HashSet<Guid> _processed = [];

    public IReadOnlySet<Guid> Snapshot => _processed;

    public void RestoreFrom(IReadOnlySet<Guid> snapshot)
    {
        _processed.Clear();
        _processed.UnionWith(snapshot);
    }

    public Task<bool> TryMarkAsProcessedAsync(
        Guid entryId,
        DateOnly competenceDate,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        Task.FromResult(_processed.Add(entryId));
}

internal sealed class FailingProcessedEntries(Exception failure) : IProcessedEntryLog
{
    public Task<bool> TryMarkAsProcessedAsync(
        Guid entryId,
        DateOnly competenceDate,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        Task.FromException<bool>(failure);
}

internal sealed class FailingBalances(IDailyBalanceRepository inner, Exception failure) : IDailyBalanceRepository
{
    public Task<DailyBalance?> FindAsync(DateOnly competenceDate, CancellationToken cancellationToken) =>
        inner.FindAsync(competenceDate, cancellationToken);

    public Task<IReadOnlyList<DailyBalance>> ListAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken) =>
        inner.ListAsync(startDate, endDate, cancellationToken);

    public Task SaveAsync(DailyBalance balance, CancellationToken cancellationToken) =>
        Task.FromException(failure);
}

internal sealed class DirectUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        operation(cancellationToken);
}

internal sealed class RollingBackUnitOfWork(InMemoryBalances balances, InMemoryProcessedEntries processedEntries)
    : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var balanceSnapshot = balances.Snapshot.ToDictionary(pair => pair.Key, pair => pair.Value);
        var processedSnapshot = processedEntries.Snapshot.ToHashSet();

        try
        {
            return await operation(cancellationToken);
        }
        catch
        {
            balances.RestoreFrom(balanceSnapshot);
            processedEntries.RestoreFrom(processedSnapshot);
            throw;
        }
    }
}

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
