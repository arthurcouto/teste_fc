namespace CashFlow.Contracts;

public sealed record EntryRecorded
{
    public const int CurrentContractVersion = 1;

    public required Guid EntryId { get; init; }

    public required EntryTypeContract Type { get; init; }

    public required decimal Amount { get; init; }

    public required DateOnly CompetenceDate { get; init; }

    public required DateTimeOffset RecordedAt { get; init; }

    public required string CorrelationId { get; init; }

    public int ContractVersion { get; init; } = CurrentContractVersion;
}
