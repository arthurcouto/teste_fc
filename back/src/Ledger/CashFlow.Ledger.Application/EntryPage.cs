using CashFlow.Ledger.Domain;

namespace CashFlow.Ledger.Application;

public sealed record EntryPage(IReadOnlyList<Entry> Entries, int TotalCount);
