using CashFlow.Ledger.Domain;

namespace CashFlow.Ledger.Infrastructure.Persistence;

internal sealed class EntryRecord
{
    public Guid Id { get; set; }

    public short Type { get; set; }

    public decimal Amount { get; set; }

    public DateOnly CompetenceDate { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset RecordedAt { get; set; }

    public static EntryRecord From(Entry entry) => new()
    {
        Id = entry.Id,
        Type = (short)entry.Type,
        Amount = entry.Amount.Amount,
        CompetenceDate = entry.CompetenceDate,
        Description = entry.Description,
        RecordedAt = entry.RecordedAt
    };

    public Entry ToAggregate() =>
        Entry.Restore(Id, (EntryType)Type, Money.Of(Amount), CompetenceDate, Description, RecordedAt);
}
