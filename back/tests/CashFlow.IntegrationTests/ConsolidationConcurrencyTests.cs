using CashFlow.Consolidation.Application;
using CashFlow.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CashFlow.IntegrationTests;

public sealed class ConsolidationConcurrencyTests(EngineFixture engine) : IClassFixture<EngineFixture>
{
    private static readonly DateTimeOffset Reference = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    private static DateOnly UniqueDate() =>
        new DateOnly(2020, 1, 1).AddDays(Random.Shared.Next(0, 2000));

    private static EntryRecorded Event(DateOnly competenceDate, decimal amount) => new()
    {
        EntryId = Guid.NewGuid(),
        Type = EntryTypeContract.Credit,
        Amount = amount,
        CompetenceDate = competenceDate,
        RecordedAt = Reference,
        CorrelationId = "integration"
    };

    private static IncorporateEntryHandler Handler(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IncorporateEntryHandler>();

    [Fact]
    public async Task TheSameEntryIsIncorporatedOnlyOnce()
    {
        if (!EngineFixture.Available)
        {
            Assert.Skip(EngineFixture.SkipReason);
        }

        var integrationEvent = Event(UniqueDate(), 50m);

        using var first = engine.Consolidation.CreateScope();
        var initial = await Handler(first).HandleAsync(integrationEvent, TestContext.Current.CancellationToken);

        using var second = engine.Consolidation.CreateScope();
        var repeated = await Handler(second).HandleAsync(integrationEvent, TestContext.Current.CancellationToken);

        initial.ShouldBe(IncorporationOutcome.Incorporated);
        repeated.ShouldBe(IncorporationOutcome.AlreadyProcessed);

        using var verification = engine.Consolidation.CreateScope();
        var balance = await verification.ServiceProvider
            .GetRequiredService<GetDailyBalanceHandler>()
            .HandleAsync(integrationEvent.CompetenceDate, TestContext.Current.CancellationToken);

        balance.EntryCount.ShouldBe(1);
        balance.Balance.ShouldBe(50m);
    }

    [Fact]
    public async Task ConcurrentDeliveriesOfTheSameEntryProduceExactlyOneWinner()
    {
        if (!EngineFixture.Available)
        {
            Assert.Skip(EngineFixture.SkipReason);
        }

        var integrationEvent = Event(UniqueDate(), 30m);
        const int deliveries = 6;

        var outcomes = await Task.WhenAll(Enumerable.Range(0, deliveries).Select(async _ =>
        {
            using var scope = engine.Consolidation.CreateScope();
            return await Handler(scope).HandleAsync(integrationEvent, TestContext.Current.CancellationToken);
        }));

        outcomes.Count(outcome => outcome == IncorporationOutcome.Incorporated).ShouldBe(1);

        using var verification = engine.Consolidation.CreateScope();
        var balance = await verification.ServiceProvider
            .GetRequiredService<GetDailyBalanceHandler>()
            .HandleAsync(integrationEvent.CompetenceDate, TestContext.Current.CancellationToken);

        balance.EntryCount.ShouldBe(1);
        balance.Balance.ShouldBe(30m);
    }

    [Fact]
    public async Task ConcurrentEntriesOnTheSameDayDoNotLoseUpdates()
    {
        if (!EngineFixture.Available)
        {
            Assert.Skip(EngineFixture.SkipReason);
        }

        var competenceDate = UniqueDate();
        const int entries = 8;
        const decimal amount = 12.50m;

        var events = Enumerable.Range(0, entries).Select(_ => Event(competenceDate, amount)).ToList();

        await Task.WhenAll(events.Select(async integrationEvent =>
        {
            using var scope = engine.Consolidation.CreateScope();
            await Handler(scope).HandleAsync(integrationEvent, TestContext.Current.CancellationToken);
        }));

        using var verification = engine.Consolidation.CreateScope();
        var balance = await verification.ServiceProvider
            .GetRequiredService<GetDailyBalanceHandler>()
            .HandleAsync(competenceDate, TestContext.Current.CancellationToken);

        balance.EntryCount.ShouldBe(entries);
        balance.TotalCredits.ShouldBe(amount * entries);
    }
}
