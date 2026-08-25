using CashFlow.Consolidation.Application;
using CashFlow.Contracts;
using Shouldly;

namespace CashFlow.Consolidation.UnitTests;

public sealed class IncorporateEntryHandlerTests
{
    private static readonly DateOnly Date = new(2026, 8, 19);
    private static readonly DateTimeOffset At = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryBalances _balances = new();
    private readonly InMemoryProcessedEntries _processed = new();
    private readonly IncorporateEntryHandler _handler;

    public IncorporateEntryHandlerTests() =>
        _handler = new IncorporateEntryHandler(_balances, _processed, new DirectUnitOfWork(), new FixedClock(At));

    private static EntryRecorded Event(Guid id, decimal amount = 100m, EntryTypeContract type = EntryTypeContract.Credit) =>
        new()
        {
            EntryId = id,
            Type = type,
            Amount = amount,
            CompetenceDate = Date,
            RecordedAt = At,
            CorrelationId = "correlation"
        };

    [Fact]
    public async Task IncorporatesUnseenEntry()
    {
        var outcome = await _handler.HandleAsync(Event(Guid.NewGuid()), TestContext.Current.CancellationToken);

        outcome.ShouldBe(IncorporationOutcome.Incorporated);
        var balance = await _balances.FindAsync(Date, TestContext.Current.CancellationToken);
        balance!.Balance.ShouldBe(100m);
    }

    [Fact]
    public async Task DiscardsEntryAlreadyProcessed()
    {
        var integrationEvent = Event(Guid.NewGuid());

        await _handler.HandleAsync(integrationEvent, TestContext.Current.CancellationToken);
        var outcome = await _handler.HandleAsync(integrationEvent, TestContext.Current.CancellationToken);

        outcome.ShouldBe(IncorporationOutcome.AlreadyProcessed);
        var balance = await _balances.FindAsync(Date, TestContext.Current.CancellationToken);
        balance!.Balance.ShouldBe(100m);
        balance.EntryCount.ShouldBe(1);
    }

    [Fact]
    public async Task ProcessingTheSameEntryManyTimesYieldsTheSameBalance()
    {
        var integrationEvent = Event(Guid.NewGuid(), 42.75m);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            await _handler.HandleAsync(integrationEvent, TestContext.Current.CancellationToken);
        }

        var balance = await _balances.FindAsync(Date, TestContext.Current.CancellationToken);
        balance!.TotalCredits.ShouldBe(42.75m);
        balance.EntryCount.ShouldBe(1);
    }

    [Fact]
    public async Task DistinctEntriesAccumulate()
    {
        await _handler.HandleAsync(Event(Guid.NewGuid(), 100m), TestContext.Current.CancellationToken);
        await _handler.HandleAsync(
            Event(Guid.NewGuid(), 30m, EntryTypeContract.Debit), TestContext.Current.CancellationToken);

        var balance = await _balances.FindAsync(Date, TestContext.Current.CancellationToken);
        balance!.Balance.ShouldBe(70m);
        balance.EntryCount.ShouldBe(2);
    }

    [Fact]
    public async Task RejectsUnsupportedContractVersion()
    {
        var integrationEvent = Event(Guid.NewGuid()) with { ContractVersion = 99 };

        await Should.ThrowAsync<UnsupportedContractVersionException>(
            () => _handler.HandleAsync(integrationEvent, TestContext.Current.CancellationToken));

        _balances.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task NothingIsPersistedWhenTheBalanceWriteFails()
    {
        var balances = new InMemoryBalances();
        var processed = new InMemoryProcessedEntries();
        var failing = new FailingBalances(balances, new InvalidOperationException("write failed"));
        var handler = new IncorporateEntryHandler(
            failing, processed, new RollingBackUnitOfWork(balances, processed), new FixedClock(At));

        var integrationEvent = Event(Guid.NewGuid());

        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(integrationEvent, TestContext.Current.CancellationToken));

        processed.Snapshot.ShouldNotContain(integrationEvent.EntryId);
        (await balances.FindAsync(Date, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ReprocessingSucceedsAfterAFailedAttempt()
    {
        var balances = new InMemoryBalances();
        var processed = new InMemoryProcessedEntries();
        var unitOfWork = new RollingBackUnitOfWork(balances, processed);
        var integrationEvent = Event(Guid.NewGuid());

        var failing = new IncorporateEntryHandler(
            new FailingBalances(balances, new InvalidOperationException("transient")),
            processed, unitOfWork, new FixedClock(At));

        await Should.ThrowAsync<InvalidOperationException>(
            () => failing.HandleAsync(integrationEvent, TestContext.Current.CancellationToken));

        var succeeding = new IncorporateEntryHandler(balances, processed, unitOfWork, new FixedClock(At));
        var outcome = await succeeding.HandleAsync(integrationEvent, TestContext.Current.CancellationToken);

        outcome.ShouldBe(IncorporationOutcome.Incorporated);
        (await balances.FindAsync(Date, TestContext.Current.CancellationToken))!.Balance.ShouldBe(100m);
    }

    [Fact]
    public async Task DoesNotWriteWhenEntryWasAlreadyProcessed()
    {
        var integrationEvent = Event(Guid.NewGuid());

        await _handler.HandleAsync(integrationEvent, TestContext.Current.CancellationToken);
        var writesAfterFirst = _balances.SaveCount;
        await _handler.HandleAsync(integrationEvent, TestContext.Current.CancellationToken);

        _balances.SaveCount.ShouldBe(writesAfterFirst);
    }
}
