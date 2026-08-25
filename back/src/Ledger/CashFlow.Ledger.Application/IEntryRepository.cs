using CashFlow.Ledger.Domain;

namespace CashFlow.Ledger.Application;

public interface IEntryRepository
{
    Task AddAsync(Entry entry, CancellationToken cancellationToken);

    Task<Entry?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<EntryPage> ListOrderedByCompetenceThenRecordedAtAsync(EntryPeriod period, CancellationToken cancellationToken);
}
