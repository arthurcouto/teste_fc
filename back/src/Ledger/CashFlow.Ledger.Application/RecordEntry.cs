using CashFlow.Contracts;
using CashFlow.Ledger.Domain;

namespace CashFlow.Ledger.Application;

public sealed record RecordEntryCommand(
    EntryType Type,
    decimal Amount,
    DateOnly CompetenceDate,
    string? Description);

public sealed class RecordEntryHandler(
    IEntryRepository repository,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICorrelationContext correlation)
{
    public Task<Entry> HandleAsync(RecordEntryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var entry = Entry.Record(
            command.Type,
            Money.Of(command.Amount),
            command.CompetenceDate,
            command.Description,
            clock.TodayAtMerchant,
            clock.UtcNow);

        return unitOfWork.ExecuteAsync(async token =>
        {
            await repository.AddAsync(entry, token);
            await outbox.AddAsync(ToIntegrationEvent(entry), token);
            return entry;
        }, cancellationToken);
    }

    private EntryRecorded ToIntegrationEvent(Entry entry) => new()
    {
        EntryId = entry.Id,
        Type = MapType(entry.Type),
        Amount = entry.Amount.Amount,
        CompetenceDate = entry.CompetenceDate,
        RecordedAt = entry.RecordedAt,
        CorrelationId = correlation.CorrelationId
    };

    private static EntryTypeContract MapType(EntryType type) => type switch
    {
        EntryType.Credit => EntryTypeContract.Credit,
        EntryType.Debit => EntryTypeContract.Debit,
        _ => throw new InvalidEntryTypeException($"Unknown entry type: {type}.")
    };
}
