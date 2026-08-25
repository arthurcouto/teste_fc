using CashFlow.Consolidation.Application;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Persistence;

internal sealed class ProcessedEntryLog(ConsolidationDbContext context) : IProcessedEntryLog
{
    public async Task<bool> TryMarkAsProcessedAsync(
        Guid entryId,
        DateOnly competenceDate,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        var inserted = await context.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO processed_entry (entry_id, competence_date, processed_at)
             VALUES ({entryId}, {competenceDate}, {at})
             ON CONFLICT (entry_id) DO NOTHING
             """,
            cancellationToken);

        return inserted == 1;
    }
}
