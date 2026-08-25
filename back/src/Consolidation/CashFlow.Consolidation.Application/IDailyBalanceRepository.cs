namespace CashFlow.Consolidation.Application;

public interface IDailyBalanceRepository
{
    Task<DailyBalance?> FindAsync(DateOnly competenceDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyBalance>> ListAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    Task SaveAsync(DailyBalance balance, CancellationToken cancellationToken);
}
