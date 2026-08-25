using CashFlow.Consolidation.Application;

namespace CashFlow.Consolidation.Infrastructure.Persistence;

internal sealed class DailyBalanceRecord
{
    public DateOnly CompetenceDate { get; set; }

    public decimal TotalCredits { get; set; }

    public decimal TotalDebits { get; set; }

    public int EntryCount { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DailyBalance ToProjection() =>
        DailyBalance.Restore(CompetenceDate, TotalCredits, TotalDebits, EntryCount, UpdatedAt);
}
