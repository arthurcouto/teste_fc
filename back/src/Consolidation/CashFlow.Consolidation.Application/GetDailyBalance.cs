namespace CashFlow.Consolidation.Application;

public sealed class GetDailyBalanceHandler(IDailyBalanceRepository balances)
{
    public async Task<DailyBalance> HandleAsync(DateOnly competenceDate, CancellationToken cancellationToken) =>
        await balances.FindAsync(competenceDate, cancellationToken) ?? DailyBalance.EmptyOn(competenceDate);
}
