namespace CashFlow.Consolidation.Application;

public sealed record BalanceSeriesQuery(DateOnly From, DateOnly To);

public sealed class GetBalanceSeriesHandler(IDailyBalanceRepository balances)
{
    public const int MaxDays = 366;

    public async Task<IReadOnlyList<DailyBalance>> HandleAsync(
        BalanceSeriesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.From > query.To)
        {
            throw new BalanceQueryException(
                $"The start date {query.From:yyyy-MM-dd} must not be later than the end date {query.To:yyyy-MM-dd}.");
        }

        var days = query.To.DayNumber - query.From.DayNumber + 1;
        if (days > MaxDays)
        {
            throw new BalanceQueryException($"The period must not exceed {MaxDays} days, but spans {days}.");
        }

        var stored = await balances.ListAsync(query.From, query.To, cancellationToken);
        var byDate = stored.ToDictionary(balance => balance.CompetenceDate);

        return Enumerable
            .Range(0, days)
            .Select(offset => query.From.AddDays(offset))
            .Select(date => byDate.TryGetValue(date, out var balance) ? balance : DailyBalance.EmptyOn(date))
            .ToList();
    }
}
