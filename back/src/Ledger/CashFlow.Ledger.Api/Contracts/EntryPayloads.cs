namespace CashFlow.Ledger.Api.Contracts;

public sealed record RecordEntryRequest(string Type, decimal Amount, DateOnly CompetenceDate, string? Description);

public sealed record EntryResponse(
    Guid Id,
    string Type,
    decimal Amount,
    DateOnly CompetenceDate,
    string? Description,
    DateTimeOffset RecordedAt);

public sealed record EntryPageResponse(IReadOnlyList<EntryResponse> Items, int TotalCount, int Offset, int Limit);
