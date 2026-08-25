using CashFlow.Ledger.Application;
using CashFlow.Ledger.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Ledger.Infrastructure.Persistence;

internal sealed class EntryRepository(LedgerDbContext context) : IEntryRepository
{
    public async Task AddAsync(Entry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await context.Entries.AddAsync(EntryRecord.From(entry), cancellationToken);
    }

    public async Task<Entry?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await context.Entries.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

        return record?.ToAggregate();
    }

    public async Task<EntryPage> ListOrderedByCompetenceThenRecordedAtAsync(
        EntryPeriod period,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(period);

        var matching = context.Entries.AsNoTracking()
            .Where(e => e.CompetenceDate >= period.From && e.CompetenceDate <= period.To);

        var total = await matching.CountAsync(cancellationToken);

        var page = await matching
            .OrderBy(e => e.CompetenceDate)
            .ThenBy(e => e.RecordedAt)
            .ThenBy(e => e.Id)
            .Skip(period.Offset)
            .Take(period.Limit)
            .ToListAsync(cancellationToken);

        return new EntryPage([.. page.Select(record => record.ToAggregate())], total);
    }
}
