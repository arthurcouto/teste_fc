using CashFlow.Consolidation.Application;
using CashFlow.Contracts;
using Shouldly;

namespace CashFlow.Consolidation.UnitTests;

public sealed class BalanceQueryTests
{
    private static readonly DateOnly Start = new(2026, 8, 17);
    private static readonly DateTimeOffset At = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBalances _balances = new();

    [Fact]
    public async Task DayWithoutMovementReturnsZeroedBalanceInsteadOfAbsence()
    {
        var handler = new GetDailyBalanceHandler(_balances);

        var balance = await handler.HandleAsync(Start, TestContext.Current.CancellationToken);

        balance.CompetenceDate.ShouldBe(Start);
        balance.Balance.ShouldBe(0m);
        balance.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SeriesIsContinuousAndFillsDaysWithoutMovement()
    {
        await _balances.SaveAsync(
            DailyBalance.EmptyOn(Start).Incorporate(EntryTypeContract.Credit, 10m, At),
            TestContext.Current.CancellationToken);
        await _balances.SaveAsync(
            DailyBalance.EmptyOn(Start.AddDays(2)).Incorporate(EntryTypeContract.Debit, 4m, At),
            TestContext.Current.CancellationToken);

        var handler = new GetBalanceSeriesHandler(_balances);
        var series = await handler.HandleAsync(
            new BalanceSeriesQuery(Start, Start.AddDays(2)), TestContext.Current.CancellationToken);

        series.Count.ShouldBe(3);
        series[0].Balance.ShouldBe(10m);
        series[1].Balance.ShouldBe(0m);
        series[1].CompetenceDate.ShouldBe(Start.AddDays(1));
        series[2].Balance.ShouldBe(-4m);
    }

    [Fact]
    public async Task RejectsInvertedPeriod()
    {
        var handler = new GetBalanceSeriesHandler(_balances);

        await Should.ThrowAsync<BalanceQueryException>(
            () => handler.HandleAsync(
                new BalanceSeriesQuery(Start.AddDays(1), Start), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RejectsPeriodAboveTheLimit()
    {
        var handler = new GetBalanceSeriesHandler(_balances);

        await Should.ThrowAsync<BalanceQueryException>(
            () => handler.HandleAsync(
                new BalanceSeriesQuery(Start, Start.AddDays(GetBalanceSeriesHandler.MaxDays)),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AcceptsPeriodAtTheLimit()
    {
        var handler = new GetBalanceSeriesHandler(_balances);

        var series = await handler.HandleAsync(
            new BalanceSeriesQuery(Start, Start.AddDays(GetBalanceSeriesHandler.MaxDays - 1)),
            TestContext.Current.CancellationToken);

        series.Count.ShouldBe(GetBalanceSeriesHandler.MaxDays);
    }
}
