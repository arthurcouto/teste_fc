using CashFlow.Consolidation.Application;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Consolidation.Infrastructure.Persistence;

internal sealed class DailyBalanceRepository(ConsolidationDbContext context, IClock clock) : IDailyBalanceRepository
{
    public async Task<DailyBalance?> FindAsync(DateOnly competenceDate, CancellationToken cancellationToken)
    {
        var record = await context.DailyBalances
            .SingleOrDefaultAsync(b => b.CompetenceDate == competenceDate, cancellationToken);

        return record?.ToProjection();
    }

    public async Task<IReadOnlyList<DailyBalance>> ListAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var records = await context.DailyBalances.AsNoTracking()
            .Where(b => b.CompetenceDate >= startDate && b.CompetenceDate <= endDate)
            .OrderBy(b => b.CompetenceDate)
            .ToListAsync(cancellationToken);

        return [.. records.Select(record => record.ToProjection())];
    }

    public async Task SaveAsync(DailyBalance balance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(balance);

        var updatedAt = balance.UpdatedAt ?? clock.UtcNow;

        await context.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO daily_balance (competence_date, total_credits, total_debits, entry_count, updated_at)
             VALUES ({balance.CompetenceDate}, {balance.TotalCredits}, {balance.TotalDebits}, {balance.EntryCount}, {updatedAt})
             ON CONFLICT (competence_date) DO UPDATE SET
                 total_credits = EXCLUDED.total_credits,
                 total_debits = EXCLUDED.total_debits,
                 entry_count = EXCLUDED.entry_count,
                 updated_at = EXCLUDED.updated_at
             """,
            cancellationToken);
    }
}
