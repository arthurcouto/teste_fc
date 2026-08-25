using CashFlow.Contracts;
using CashFlow.Ledger.Application;
using CashFlow.Ledger.Domain;
using Shouldly;

namespace CashFlow.Ledger.UnitTests;

public sealed class RecordEntryHandlerTests
{
    private static readonly DateOnly Today = new(2026, 8, 19);
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 14, 30, 0, TimeSpan.Zero);
    private const string Correlation = "correlation-id";

    private readonly RecordingRepository _repository = new();
    private readonly RecordingOutbox _outbox = new();
    private readonly RollingBackUnitOfWork _unitOfWork;
    private readonly RecordEntryHandler _handler;

    public RecordEntryHandlerTests()
    {
        _unitOfWork = new RollingBackUnitOfWork(_repository, _outbox);
        _handler = new RecordEntryHandler(
            _repository,
            _outbox,
            _unitOfWork,
            new FixedClock(Now, Today),
            new FixedCorrelation(Correlation));
    }

    private static RecordEntryCommand Command(
        EntryType type = EntryType.Credit,
        decimal amount = 100m,
        DateOnly? competenceDate = null) =>
        new(type, amount, competenceDate ?? Today, "daily sale");

    [Fact]
    public async Task PersistsTheEntryAndTheIntegrationEventTogether()
    {
        await _handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        _repository.Entries.Count.ShouldBe(1);
        _outbox.Events.Count.ShouldBe(1);
        _unitOfWork.Executions.ShouldBe(1);
    }

    [Fact]
    public async Task NothingIsPersistedWhenTheOutboxWriteFails()
    {
        var handler = new RecordEntryHandler(
            _repository,
            new FailingOutbox(_outbox, new TimeoutException("outbox unavailable")),
            _unitOfWork,
            new FixedClock(Now, Today),
            new FixedCorrelation(Correlation));

        await Should.ThrowAsync<TimeoutException>(
            () => handler.HandleAsync(Command(), TestContext.Current.CancellationToken));

        _unitOfWork.Executions.ShouldBe(1);
        _repository.Entries.ShouldBeEmpty();
        _outbox.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheEntryAndTheIntegrationEventAreVisibleOnlyAfterTheUnitOfWorkCommits()
    {
        var outbox = new RecordingOutbox();
        var repository = new RecordingRepository();
        var unitOfWork = new RollingBackUnitOfWork(repository, outbox);
        var handler = new RecordEntryHandler(
            repository, outbox, unitOfWork, new FixedClock(Now, Today), new FixedCorrelation(Correlation));

        await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        var failing = new RecordEntryHandler(
            repository,
            new FailingOutbox(outbox, new TimeoutException("outbox unavailable")),
            unitOfWork,
            new FixedClock(Now, Today),
            new FixedCorrelation(Correlation));

        await Should.ThrowAsync<TimeoutException>(
            () => failing.HandleAsync(Command(), TestContext.Current.CancellationToken));

        repository.Entries.Count.ShouldBe(1);
        outbox.Events.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TheIntegrationEventCarriesEverythingTheConsumerNeeds()
    {
        var entry = await _handler.HandleAsync(
            Command(EntryType.Debit, 42.75m, Today.AddDays(-1)), TestContext.Current.CancellationToken);

        var published = _outbox.Events.Single();

        published.EntryId.ShouldBe(entry.Id);
        published.Type.ShouldBe(EntryTypeContract.Debit);
        published.Amount.ShouldBe(42.75m);
        published.CompetenceDate.ShouldBe(Today.AddDays(-1));
        published.RecordedAt.ShouldBe(Now);
        published.CorrelationId.ShouldBe(Correlation);
        published.ContractVersion.ShouldBe(EntryRecorded.CurrentContractVersion);
    }

    [Fact]
    public async Task MapsCreditWithoutFallingBackToDebit()
    {
        await _handler.HandleAsync(Command(EntryType.Credit), TestContext.Current.CancellationToken);

        _outbox.Events.Single().Type.ShouldBe(EntryTypeContract.Credit);
    }

    [Fact]
    public async Task RejectsInvalidAmountBeforeOpeningTheUnitOfWork()
    {
        await Should.ThrowAsync<InvalidMoneyException>(
            () => _handler.HandleAsync(Command(amount: 0m), TestContext.Current.CancellationToken));

        _unitOfWork.Executions.ShouldBe(0);
        _repository.Entries.ShouldBeEmpty();
        _outbox.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task RejectsFutureCompetenceBeforeOpeningTheUnitOfWork()
    {
        await Should.ThrowAsync<InvalidCompetenceDateException>(
            () => _handler.HandleAsync(
                Command(competenceDate: Today.AddDays(1)), TestContext.Current.CancellationToken));

        _unitOfWork.Executions.ShouldBe(0);
    }

    [Fact]
    public async Task RejectsNullCommand() =>
        await Should.ThrowAsync<ArgumentNullException>(
            () => _handler.HandleAsync(null!, TestContext.Current.CancellationToken));
}
