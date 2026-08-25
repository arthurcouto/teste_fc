using CashFlow.Ledger.Application;
using CashFlow.Ledger.Domain;

namespace CashFlow.Ledger.Api.Contracts;

internal static class EntryMapping
{
    public static EntryType ToEntryType(string value) => value?.ToLowerInvariant() switch
    {
        "credit" => EntryType.Credit,
        "debit" => EntryType.Debit,
        _ => throw new RequestValidationException(
            $"Entry type must be either credit or debit, but was '{value}'.")
    };

    public static EntryResponse ToResponse(this Entry entry) => new(
        entry.Id,
        entry.Type.ToString().ToLowerInvariant(),
        entry.Amount.Amount,
        entry.CompetenceDate,
        entry.Description,
        entry.RecordedAt);

    public static EntryPageResponse ToResponse(this EntryPage page, EntryPeriod period) => new(
        [.. page.Entries.Select(ToResponse)],
        page.TotalCount,
        period.Offset,
        period.Limit);
}
