using CashFlow.Contracts;
using CashFlow.Ledger.Application;
using CashFlow.Ledger.Domain;

namespace CashFlow.Ledger.UnitTests;

internal sealed class RecordingRepository : IEntryRepository
{
    private readonly List<Entry> _entries = [];

    public IReadOnlyList<Entry> Entries => _entries;

    public void RestoreFrom(IReadOnlyList<Entry> snapshot)
    {
        _entries.Clear();
        _entries.AddRange(snapshot);
    }

    public Task AddAsync(Entry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<Entry?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_entries.SingleOrDefault(e => e.Id == id));

    public Task<EntryPage> ListOrderedByCompetenceThenRecordedAtAsync(
        EntryPeriod period,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class RecordingOutbox : IOutbox
{
    private readonly List<EntryRecorded> _events = [];

    public IReadOnlyList<EntryRecorded> Events => _events;

    public void RestoreFrom(IReadOnlyList<EntryRecorded> snapshot)
    {
        _events.Clear();
        _events.AddRange(snapshot);
    }

    public Task AddAsync(EntryRecorded integrationEvent, CancellationToken cancellationToken)
    {
        _events.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

internal sealed class FailingOutbox(RecordingOutbox inner, Exception failure) : IOutbox
{
    public async Task AddAsync(EntryRecorded integrationEvent, CancellationToken cancellationToken)
    {
        await inner.AddAsync(integrationEvent, cancellationToken);

        throw failure;
    }
}

internal sealed class RollingBackUnitOfWork(RecordingRepository repository, RecordingOutbox outbox) : IUnitOfWork
{
    public int Executions { get; private set; }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        Executions++;
        var entries = repository.Entries.ToList();
        var events = outbox.Events.ToList();

        try
        {
            return await operation(cancellationToken);
        }
        catch
        {
            repository.RestoreFrom(entries);
            outbox.RestoreFrom(events);
            throw;
        }
    }
}

internal sealed class FixedClock(DateTimeOffset utcNow, DateOnly todayAtMerchant) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;

    public DateOnly TodayAtMerchant { get; } = todayAtMerchant;
}

internal sealed class FixedCorrelation(string correlationId) : ICorrelationContext
{
    public string CorrelationId { get; } = correlationId;
}
