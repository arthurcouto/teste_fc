namespace CashFlow.Ledger.Application;

public sealed record ListEntriesQuery(DateOnly From, DateOnly To, int? Offset, int? Limit);

public sealed class ListEntriesHandler(IEntryRepository repository)
{
    public Task<EntryPage> HandleAsync(ListEntriesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var period = EntryPeriod.Of(query.From, query.To, query.Offset, query.Limit);

        return repository.ListOrderedByCompetenceThenRecordedAtAsync(period, cancellationToken);
    }
}
