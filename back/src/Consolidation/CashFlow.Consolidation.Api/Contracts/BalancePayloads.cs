using CashFlow.Consolidation.Application;

namespace CashFlow.Consolidation.Api.Contracts;

public sealed record DailyBalanceResponse(
    DateOnly CompetenceDate,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal Balance,
    int EntryCount,
    DateTimeOffset? UpdatedAt);

internal static class BalanceMapping
{
    public static DailyBalanceResponse ToResponse(this DailyBalance balance) => new(
        balance.CompetenceDate,
        balance.TotalCredits,
        balance.TotalDebits,
        balance.Balance,
        balance.EntryCount,
        balance.UpdatedAt);
}
