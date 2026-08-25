namespace CashFlow.Ledger.Domain;

public sealed class Entry
{
    public const int DescriptionMaxLength = 200;

    private Entry(
        Guid id,
        EntryType type,
        Money amount,
        DateOnly competenceDate,
        string? description,
        DateTimeOffset recordedAt)
    {
        Id = id;
        Type = type;
        Amount = amount;
        CompetenceDate = competenceDate;
        Description = description;
        RecordedAt = recordedAt;
    }

    public Guid Id { get; }

    public EntryType Type { get; }

    public Money Amount { get; }

    public DateOnly CompetenceDate { get; }

    public string? Description { get; }

    public DateTimeOffset RecordedAt { get; }

    public static Entry Record(
        EntryType type,
        Money amount,
        DateOnly competenceDate,
        string? description,
        DateOnly currentDateAtMerchant,
        DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (!Enum.IsDefined(type))
        {
            throw new InvalidEntryTypeException($"Unknown entry type: {type}.");
        }

        if (competenceDate > currentDateAtMerchant)
        {
            throw new InvalidCompetenceDateException(
                $"Competence date {competenceDate:yyyy-MM-dd} is later than the current date {currentDateAtMerchant:yyyy-MM-dd}.");
        }

        var normalizedDescription = NormalizeDescription(description);

        return new Entry(Guid.NewGuid(), type, amount, competenceDate, normalizedDescription, recordedAt);
    }

    public static Entry Restore(
        Guid id,
        EntryType type,
        Money amount,
        DateOnly competenceDate,
        string? description,
        DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (id == Guid.Empty)
        {
            throw new CorruptedEntryException("A stored entry must have a non-empty identifier.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new CorruptedEntryException($"A stored entry has an unknown type: {type}.");
        }

        if (description is { Length: > DescriptionMaxLength })
        {
            throw new CorruptedEntryException(
                $"A stored entry has a description longer than {DescriptionMaxLength} characters.");
        }

        return new Entry(id, type, amount, competenceDate, description, recordedAt);
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();

        if (trimmed.Length > DescriptionMaxLength)
        {
            throw new InvalidDescriptionException(
                $"Entry description must have at most {DescriptionMaxLength} characters.");
        }

        return trimmed;
    }
}
